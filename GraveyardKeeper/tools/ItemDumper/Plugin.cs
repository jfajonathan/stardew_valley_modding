using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;

[BepInPlugin("tmp.gyk.itemdumper", "GYK Item Dumper", "0.0.1")]
public class Plugin : BaseUnityPlugin
{
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(15f);
        var sb = new StringBuilder();
        sb.AppendLine("id\tname\tstack_count\ttype\titem_size\thas_durability\tis_bag\tbag_type\tin_bags\tq_plus\tq_minus\tdur_craft\tnot_used");
        var durIds = new System.Collections.Generic.HashSet<string>();
        foreach (var c in GameBalance.me.craft_data)
        {
            try { if (c != null && c.takes_item_durability && c.needs.Count > c.dur_needs_item_index) durIds.Add(c.needs[c.dur_needs_item_index].id); } catch { }
        }
        foreach (var d in GameBalance.me.items_data)
        {
            if (d == null) continue;
            string name;
            try { name = d.GetItemName(true); } catch { name = "?"; }
            sb.Append(d.id).Append('\t').Append((name ?? "").Replace('\t', ' ').Replace('\n', ' ')).Append('\t')
              .Append(d.stack_count).Append('\t').Append(d.type).Append('\t').Append(d.item_size).Append('\t')
              .Append(d.has_durability).Append('\t').Append(d.is_bag).Append('\t').Append(d.bag_type).Append('\t')
              .Append(string.Join(",", d.can_be_inserted_in_bag.Select(b => b.ToString()).ToArray())).Append('\t')
              .Append(d.q_plus).Append('\t').Append(d.q_minus).Append('\t').Append(durIds.Contains(d.id)).Append('\t').Append(d.not_used)
              .AppendLine();
        }
        var path = Path.Combine(Paths.BepInExRootPath, "gyk_items_dump.tsv");
        File.WriteAllText(path, sb.ToString());
        Logger.LogMessage("Dumped " + GameBalance.me.items_data.Count + " items to " + path);
    }
}
