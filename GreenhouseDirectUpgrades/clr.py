import struct, sys

class Reader:
    def __init__(self, data):
        self.d = data
    def u8(self, o): return self.d[o]
    def u16(self, o): return struct.unpack_from('<H', self.d, o)[0]
    def u32(self, o): return struct.unpack_from('<I', self.d, o)[0]
    def u64(self, o): return struct.unpack_from('<Q', self.d, o)[0]

def load(path):
    with open(path, 'rb') as f:
        return f.read()

TABLE_NAMES = {
 0x00:'Module',0x01:'TypeRef',0x02:'TypeDef',0x04:'Field',0x06:'MethodDef',
 0x08:'Param',0x09:'InterfaceImpl',0x0A:'MemberRef',0x0B:'Constant',
 0x0C:'CustomAttribute',0x0D:'FieldMarshal',0x0E:'DeclSecurity',
 0x0F:'ClassLayout',0x10:'FieldLayout',0x11:'StandAloneSig',0x12:'EventMap',
 0x14:'Event',0x15:'PropertyMap',0x17:'Property',0x18:'MethodSemantics',
 0x19:'MethodImpl',0x1A:'ModuleRef',0x1B:'TypeSpec',0x1C:'ImplMap',
 0x1D:'FieldRVA',0x20:'Assembly',0x21:'AssemblyProcessor',0x22:'AssemblyOS',
 0x23:'AssemblyRef',0x24:'AssemblyRefProcessor',0x25:'AssemblyRefOS',
 0x26:'File',0x27:'ExportedType',0x28:'ManifestResource',0x29:'NestedClass',
 0x2A:'GenericParam',0x2B:'MethodSpec',0x2C:'GenericParamConstraint',
}

CODED_INDEX_DEFS = {
 'TypeDefOrRef': (2, [0x02,0x01,0x1B]),
 'HasConstant': (2, [0x04,0x08,0x17]),
 'HasCustomAttribute': (5, [0x06,0x04,0x02,0x08,0x09,0x0A,0x11,0x14,0x17,0x00,0x0C,0x23,0x1A,0x1B,0x20,0x26,0x27,0x28,0x02,0x2A,0x2B,0x2C]),
 'HasFieldMarshal': (1, [0x04,0x08]),
 'HasDeclSecurity': (2, [0x02,0x06,0x20]),
 'MemberRefParent': (3, [0x02,0x01,0x1A,0x06,0x1B]),
 'HasSemantics': (1, [0x17,0x14]),
 'MethodDefOrRef': (1, [0x06,0x0A]),
 'MemberForwarded': (1, [0x04,0x06]),
 'Implementation': (2, [0x26,0x23,0x27]),
 'CustomAttributeType': (3, [0x02,0x01,0x06,0x0A,0x00]),
 'ResolutionScope': (2, [0x00,0x1A,0x23,0x01]),
 'TypeOrMethodDef': (1, [0x02,0x06]),
}

class Assembly:
    def __init__(self, data):
        self.data = data
        self.r = Reader(data)
        self.parse()

    def parse(self):
        r, d = self.r, self.data
        e_lfanew = r.u32(0x3C)
        pe = e_lfanew
        assert d[pe:pe+4] == b'PE\x00\x00'
        coff = pe+4
        num_sections = r.u16(coff+2)
        opt_hdr_size = r.u16(coff+16)
        opt_hdr = coff+20
        magic = r.u16(opt_hdr)
        is_pe32plus = (magic == 0x20B)
        if is_pe32plus:
            rva_dd_offset = opt_hdr + 112
        else:
            rva_dd_offset = opt_hdr + 96
        # data directory 14 = COM descriptor (CLI header)
        cli_dir_off = rva_dd_offset + 14*8
        cli_rva = r.u32(cli_dir_off)
        cli_size = r.u32(cli_dir_off+4)

        sec_start = opt_hdr + opt_hdr_size
        self.sections = []
        for i in range(num_sections):
            off = sec_start + i*40
            name = d[off:off+8].rstrip(b'\x00').decode('ascii', 'replace')
            vsize = r.u32(off+8)
            vaddr = r.u32(off+12)
            rawsize = r.u32(off+16)
            rawptr = r.u32(off+20)
            self.sections.append((name, vaddr, vsize, rawptr, rawsize))

        self.cli_rva = cli_rva
        cli_off = self.rva2off(cli_rva)
        self.cli_off = cli_off
        # CLI header fields
        self.cli_cb = r.u32(cli_off)
        self.meta_rva = r.u32(cli_off+8)
        self.meta_size = r.u32(cli_off+12)
        self.entrypoint_token = r.u32(cli_off+20)

        meta_off = self.rva2off(self.meta_rva)
        self.meta_off = meta_off
        assert d[meta_off:meta_off+4] == b'BSJB', d[meta_off:meta_off+4]
        version_len = r.u32(meta_off+12)
        # pad to 4
        vlen_padded = (version_len + 3) & ~3
        after_version = meta_off+16+vlen_padded
        flags = r.u16(after_version)
        num_streams = r.u16(after_version+2)
        p = after_version+4
        self.streams = {}
        for i in range(num_streams):
            s_off = r.u32(p)
            s_size = r.u32(p+4)
            p += 8
            name_start = p
            end = d.index(b'\x00', name_start)
            name = d[name_start:end].decode('ascii')
            namelen_padded = ((end - name_start + 1) + 3) & ~3
            p = name_start + namelen_padded
            self.streams[name] = (meta_off + s_off, s_size)

        self.strings_off, self.strings_size = self.streams.get('#Strings', (0,0))
        self.us_off, self.us_size = self.streams.get('#US', (0,0))
        self.guid_off, self.guid_size = self.streams.get('#GUID', (0,0))
        self.blob_off, self.blob_size = self.streams.get('#Blob', (0,0))
        tables_key = '#~' if '#~' in self.streams else '#-'
        self.tables_off, self.tables_size = self.streams[tables_key]
        self.parse_tables()

    def rva2off(self, rva):
        for name, vaddr, vsize, rawptr, rawsize in self.sections:
            if vaddr <= rva < vaddr + max(vsize, rawsize):
                return rawptr + (rva - vaddr)
        raise ValueError(f'RVA {rva:x} not found in any section')

    def off2rva(self, off):
        for name, vaddr, vsize, rawptr, rawsize in self.sections:
            if rawptr <= off < rawptr + rawsize:
                return vaddr + (off - rawptr)
        raise ValueError(f'file offset {off:x} not found in any section')

    def get_string(self, idx):
        if idx == 0:
            return ''
        off = self.strings_off + idx
        end = self.data.index(b'\x00', off)
        return self.data[off:end].decode('utf-8', 'replace')

    def get_us(self, idx):
        # user string heap: length-prefixed (compressed int), utf-16, +1 trailing byte
        off = self.us_off + idx
        length, off2 = self.read_compressed(off)
        if length == 0:
            return ''
        raw = self.data[off2:off2+length-1]
        return raw.decode('utf-16-le', 'replace')

    def get_blob(self, idx):
        if idx == 0:
            return b''
        off = self.blob_off + idx
        length, off2 = self.read_compressed(off)
        return self.data[off2:off2+length]

    def read_compressed(self, off):
        b0 = self.data[off]
        if b0 & 0x80 == 0:
            return b0, off+1
        elif b0 & 0xC0 == 0x80:
            val = ((b0 & 0x3F) << 8) | self.data[off+1]
            return val, off+2
        else:
            val = ((b0 & 0x1F) << 24) | (self.data[off+1] << 16) | (self.data[off+2] << 8) | self.data[off+3]
            return val, off+4

    def parse_tables(self):
        r = self.r
        off = self.tables_off
        reserved = r.u32(off)
        major = r.u8(off+4)
        minor = r.u8(off+5)
        heap_sizes = r.u8(off+6)
        reserved2 = r.u8(off+7)
        valid = r.u64(off+8)
        sorted_ = r.u64(off+16)
        p = off + 24
        self.row_counts = {}
        present = []
        for i in range(64):
            if valid & (1 << i):
                present.append(i)
        for tid in present:
            cnt = r.u32(p)
            self.row_counts[tid] = cnt
            p += 4

        self.str_idx_size = 4 if (heap_sizes & 0x1) else 2
        self.guid_idx_size = 4 if (heap_sizes & 0x2) else 2
        self.blob_idx_size = 4 if (heap_sizes & 0x4) else 2

        def tbl_idx_size(tid):
            cnt = self.row_counts.get(tid, 0)
            return 4 if cnt > 0xFFFF else 2

        def coded_idx_size(name):
            bits, tids = CODED_INDEX_DEFS[name]
            maxrows = max((self.row_counts.get(t,0) for t in tids), default=0)
            return 4 if maxrows > (0xFFFF >> bits) else 2

        self.tbl_idx_size = tbl_idx_size
        self.coded_idx_size = coded_idx_size

        # Define row layouts: list of ('field', kind) where kind in
        # 'u2','u4','str','guid','blob', ('tbl', tableid), ('coded', name)
        L = lambda *cols: cols
        self.layouts = {
         0x00: L(('Generation','u2'),('Name','str'),('Mvid','guid'),('EncId','guid'),('EncBaseId','guid')),
         0x01: L(('ResolutionScope',('coded','ResolutionScope')),('Name','str'),('Namespace','str')),
         0x02: L(('Flags','u4'),('Name','str'),('Namespace','str'),('Extends',('coded','TypeDefOrRef')),('FieldList',('tbl',0x04)),('MethodList',('tbl',0x06))),
         0x04: L(('Flags','u2'),('Name','str'),('Signature','blob')),
         0x06: L(('RVA','u4'),('ImplFlags','u2'),('Flags','u2'),('Name','str'),('Signature','blob'),('ParamList',('tbl',0x08))),
         0x08: L(('Flags','u2'),('Sequence','u2'),('Name','str')),
         0x09: L(('Class',('tbl',0x02)),('Interface',('coded','TypeDefOrRef'))),
         0x0A: L(('Class',('coded','MemberRefParent')),('Name','str'),('Signature','blob')),
         0x0B: L(('Type','u2'),('Parent',('coded','HasConstant')),('Value','blob')),
         0x0C: L(('Parent',('coded','HasCustomAttribute')),('Type',('coded','CustomAttributeType')),('Value','blob')),
         0x0D: L(('Parent',('coded','HasFieldMarshal')),('NativeType','blob')),
         0x0E: L(('Action','u2'),('Parent',('coded','HasDeclSecurity')),('PermissionSet','blob')),
         0x0F: L(('PackingSize','u2'),('ClassSize','u4'),('Parent',('tbl',0x02))),
         0x10: L(('Offset','u4'),('Field',('tbl',0x04))),
         0x11: L(('Signature','blob'),),
         0x12: L(('Parent',('tbl',0x02)),('EventList',('tbl',0x14))),
         0x14: L(('EventFlags','u2'),('Name','str'),('EventType',('coded','TypeDefOrRef'))),
         0x15: L(('Parent',('tbl',0x02)),('PropertyList',('tbl',0x17))),
         0x17: L(('Flags','u2'),('Name','str'),('Type','blob')),
         0x18: L(('Semantics','u2'),('Method',('tbl',0x06)),('Association',('coded','HasSemantics'))),
         0x19: L(('Class',('tbl',0x02)),('MethodBody',('coded','MethodDefOrRef')),('MethodDeclaration',('coded','MethodDefOrRef'))),
         0x1A: L(('Name','str'),),
         0x1B: L(('Signature','blob'),),
         0x1C: L(('MappingFlags','u2'),('MemberForwarded',('coded','MemberForwarded')),('ImportName','str'),('ImportScope',('tbl',0x1A))),
         0x1D: L(('RVA','u4'),('Field',('tbl',0x04))),
         0x20: L(('HashAlgId','u4'),('MajorVersion','u2'),('MinorVersion','u2'),('BuildNumber','u2'),('RevisionNumber','u2'),('Flags','u4'),('PublicKey','blob'),('Name','str'),('Culture','str')),
         0x23: L(('MajorVersion','u2'),('MinorVersion','u2'),('BuildNumber','u2'),('RevisionNumber','u2'),('Flags','u4'),('PublicKeyOrToken','blob'),('Name','str'),('Culture','str'),('HashValue','blob')),
         0x26: L(('Flags','u4'),('Name','str'),('HashValue','blob')),
         0x27: L(('Flags','u4'),('TypeDefId','u4'),('TypeName','str'),('TypeNamespace','str'),('Implementation',('coded','Implementation'))),
         0x28: L(('Offset','u4'),('Flags','u4'),('Name','str'),('Implementation',('coded','Implementation'))),
         0x29: L(('NestedClass',('tbl',0x02)),('EnclosingClass',('tbl',0x02))),
         0x2A: L(('Number','u2'),('Flags','u2'),('Owner',('coded','TypeOrMethodDef')),('Name','str')),
         0x2B: L(('Method',('coded','MethodDefOrRef')),('Instantiation','blob')),
         0x2C: L(('Owner',('tbl',0x2A)),('Constraint',('coded','TypeDefOrRef'))),
        }

        # compute row size for each present table
        def field_size(kind):
            if kind == 'u2': return 2
            if kind == 'u4': return 4
            if kind == 'str': return self.str_idx_size
            if kind == 'guid': return self.guid_idx_size
            if kind == 'blob': return self.blob_idx_size
            if isinstance(kind, tuple):
                if kind[0] == 'tbl': return tbl_idx_size(kind[1])
                if kind[0] == 'coded': return coded_idx_size(kind[1])
            raise ValueError(kind)

        self.tables = {}
        p2 = p
        for tid in present:
            cnt = self.row_counts[tid]
            layout = self.layouts[tid]
            row_size = sum(field_size(k) for _, k in layout)
            rows = []
            base = p2
            for i in range(cnt):
                roff = base + i*row_size
                row = {}
                fo = roff
                for fname, kind in layout:
                    sz = field_size(kind)
                    if sz == 2:
                        val = r.u16(fo)
                    else:
                        val = r.u32(fo)
                    row[fname] = (val, kind)
                    fo += sz
                rows.append(row)
            self.tables[tid] = rows
            p2 += cnt * row_size

        self.tables_end_off = p2

    def method_body_offset(self, rva):
        if rva == 0:
            return None
        return self.rva2off(rva)

    def read_method_il(self, rva):
        off = self.method_body_offset(rva)
        if off is None:
            return None, None, None
        b0 = self.data[off]
        flag = b0 & 0x3
        if flag == 0x2:  # tiny format
            code_size = b0 >> 2
            header_size = 1
            max_stack = 8
            local_var_sig_tok = 0
        else:  # fat format
            b1 = self.data[off+1]
            header_size_words = (b1 >> 4) & 0xF
            header_size = header_size_words * 4
            max_stack = struct.unpack_from('<H', self.data, off+2)[0]
            code_size = struct.unpack_from('<I', self.data, off+4)[0]
            local_var_sig_tok = struct.unpack_from('<I', self.data, off+8)[0]
        il_off = off + header_size
        il_bytes = self.data[il_off:il_off+code_size]
        return il_off, il_bytes, header_size

    def resolve_string_table(self, tid):
        return TABLE_NAMES.get(tid, hex(tid))
