"""
Patch script for NewGreenhouseUpgrades.dll (LovelyChaoAndBpendragon.NewGreenhouseUpgrades, v1.0.0 / asm v2.1.1)

Goal: make the greenhouse upgrade blueprints show up in Robin's carpenter menu using
ONLY the same prerequisite vanilla building upgrades use (owning the previous tier),
plus "the Greenhouse itself must be unlocked" (ccPantry/jojaPantry) for tier 1 -
with NO wizard friendship requirement and NO next-day mail delay.

Background (reverse engineered with clr.py/ildasm.py, a from-scratch pure-Python
ECMA-335 metadata reader + IL disassembler, since no .NET SDK/decompiler was reachable
in the build environment):

- The mod registers a custom GameStateQuery named "GreenHouseSprinklers.BuildCondition"
  (see ModEntry.Entry, call to StardewValley.GameStateQuery.Register). This query is
  used as the `BuildCondition` for all 3 injected Data/Buildings rows
  ("GreenhouseSprinklers.Upgrade1/2/3"), and the game evaluates it LIVE every time
  Robin's shop menu opens - exactly like vanilla building BuildConditions.
- The query's implementation is `CheckForUpgrade(string[] query, GameStateQueryContext ctx)`.
  It parses the requested tier number N from the query args, requires
  `GetUpgradeLevel(greenhouseBuilding) + 1 == N` (i.e. "this is the next tier" -
  this is what makes tier 2/3 only show once the previous tier is actually built,
  the same mechanism as Coop -> Big Coop -> Deluxe Coop), AND ALSO required
  `Game1.player.mailReceived.Contains("Bpendragon.GreenhouseSprinklers.Wizard{N}")`
  or the "...{N}b" variant - i.e. "the wizard's letter for this tier was received".
- That letter is only ever queued (via `Game1.addMailForTomorrow`, so it always takes
  a full day to arrive) by `AddLetterIfNeeded`, itself only called from `OnDayEnding`
  once the greenhouse is unlocked. `AddLetterIfNeeded` additionally required the player
  to have `Game1.player.friendshipData` contain an entry for "Wizard" (i.e. to have
  met/talked to the Wizard at least once) before even considering the Hearts-based
  friendship threshold from config.json.

This patch:
  1. (already applied previously) Neutralizes the "has talked to the Wizard" gate in
     AddLetterIfNeeded so it's always treated as satisfied.
  2. Rewrites CheckForUpgrade so the two `mailReceived.Contains(...)` checks test the
     literal strings "ccPantry" / "jojaPantry" (the same flags OnDayEnding already
     uses to decide "is the Greenhouse unlocked") instead of the Wizard-mail flags.
     Combined with the pre-existing "is this the next tier" check, this reproduces
     the exact behavior of a native Data/Buildings BuildingToUpgrade chain: tier 1
     needs only "Greenhouse unlocked", tier 2/3 need only "own the previous tier" -
     evaluated live, no waiting for any letter.
  3. Removes the (now pointless) call to AddLetterIfNeeded from OnDayEnding, so the
     mod no longer sends the Wizard letters at all.

Every edit below is a same-length byte substitution (opcodes/NOPs only), so no RVA,
metadata offset, or PE header needs to change - the file size and the layout of every
other method are untouched. Diffed byte-for-byte against the shipped
NewGreenhouseUpgrades.dll 1.0.0 (asm v2.1.1) and re-parsed/re-disassembled in full
(117 methods) after each step to confirm the file stays well-formed.
"""
import struct, sys
sys.path.insert(0, __file__.rsplit('/', 1)[0])
from clr import Assembly, load

def patch(in_path, out_path):
    data = bytearray(load(in_path))
    asm = Assembly(bytes(data))
    methods = asm.tables[0x06]

    def find(name):
        for row in methods:
            if asm.get_string(row['Name'][0]) == name:
                return row
        raise KeyError(name)

    def il_of(name):
        row = find(name)
        return asm.read_method_il(row['RVA'][0])  # il_off, il_bytes, hdrsize

    # --- Step 1: AddLetterIfNeeded - force "has talked to wizard" to always be true ---
    _, ilb, _ = il_of('AddLetterIfNeeded')
    rel = 0x0131
    assert ilb[rel:rel+3] == bytes.fromhex('16fe01'), "unexpected bytes; DLL version mismatch?"
    il_off = il_of('AddLetterIfNeeded')[0]
    abs_off = il_off + rel
    assert bytes(data[abs_off:abs_off+3]) == bytes.fromhex('16fe01')
    data[abs_off:abs_off+3] = bytes.fromhex('260016')  # pop; nop; ldc.i4.0

    # re-parse for later steps (offsets below are independent of step 1, but re-derive
    # tokens/il_off fresh from the mutated buffer for safety)
    asm = Assembly(bytes(data))
    methods = asm.tables[0x06]

    # --- get the existing 'ccPantry' / 'jojaPantry' ldstr tokens from OnDayEnding ---
    ode_row = find('OnDayEnding')
    ode_off, ode_bytes, _ = asm.read_method_il(ode_row['RVA'][0])
    cc_bytes = ode_bytes[0x54:0x54+5]
    joja_bytes = ode_bytes[0x65:0x65+5]
    assert cc_bytes[0] == 0x72 and joja_bytes[0] == 0x72
    cc_tok = struct.unpack_from('<I', cc_bytes, 1)[0]
    joja_tok = struct.unpack_from('<I', joja_bytes, 1)[0]
    assert asm.get_us(cc_tok & 0xFFFFFF) == 'ccPantry'
    assert asm.get_us(joja_tok & 0xFFFFFF) == 'jojaPantry'

    def mk_ldstr(tok):
        return bytes([0x72]) + struct.pack('<I', tok)

    # --- Step 2: CheckForUpgrade - swap Wizard-mail checks for ccPantry/jojaPantry ---
    cfu_row = find('CheckForUpgrade')
    cfu_off, cfu_bytes, _ = asm.read_method_il(cfu_row['RVA'][0])

    b1_start, b1_end = 0x8F, 0xB6
    region1 = cfu_bytes[b1_start:b1_end]
    assert len(region1) == 39 and region1[0] == 0x12  # ldloca.s ... (sanity)
    new1 = mk_ldstr(cc_tok) + bytes(len(region1) - 5)
    abs1 = cfu_off + b1_start
    assert bytes(data[abs1:abs1+len(region1)]) == bytes(region1)
    data[abs1:abs1+len(new1)] = new1

    b2_start, b2_end = 0xC7, 0xFB
    region2 = cfu_bytes[b2_start:b2_end]
    assert len(region2) == 52 and region2[0] == 0x12
    new2 = mk_ldstr(joja_tok) + bytes(len(region2) - 5)
    abs2 = cfu_off + b2_start
    assert bytes(data[abs2:abs2+len(region2)]) == bytes(region2)
    data[abs2:abs2+len(new2)] = new2

    # --- Step 3: OnDayEnding - stop calling AddLetterIfNeeded (no longer needed) ---
    asm = Assembly(bytes(data))
    methods = asm.tables[0x06]
    ode_row = find('OnDayEnding')
    ode_off, ode_bytes, _ = asm.read_method_il(ode_row['RVA'][0])
    span_start, span_end = 0x91, 0x9D
    region3 = ode_bytes[span_start:span_end]
    assert region3.hex() == '02062824000006281d000006'
    abs3 = ode_off + span_start
    assert bytes(data[abs3:abs3+len(region3)]) == bytes(region3)
    data[abs3:abs3+len(region3)] = bytes(len(region3))

    with open(out_path, 'wb') as f:
        f.write(data)

    # final sanity: re-parse & re-disassemble every method
    asm = Assembly(bytes(data))
    from ildasm import decode
    methods = asm.tables[0x06]
    for row in methods:
        rva = row['RVA'][0]
        if rva == 0:
            continue
        _, ilb, _ = asm.read_method_il(rva)
        decode(ilb, asm)  # raises on malformed IL
    print(f"OK - wrote {out_path} ({len(data)} bytes, {len(methods)} methods re-verified)")

if __name__ == '__main__':
    patch(sys.argv[1], sys.argv[2])
