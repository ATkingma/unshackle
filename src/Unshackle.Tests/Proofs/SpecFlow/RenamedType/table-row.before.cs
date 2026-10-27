using System.Collections.Generic;
using TechTalk.SpecFlow;

namespace Shop.Specs;

public static class Rows
{
    public static List<string> Names(Table table)
    {
        var names = new List<string>();
        foreach (TableRow row in table.Rows)
        {
            names.Add(row["Name"]);
        }

        return names;
    }
}
