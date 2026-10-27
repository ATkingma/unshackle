using System.Collections.Generic;
using Reqnroll;

namespace Shop.Specs;

public static class Rows
{
    public static List<string> Names(Table table)
    {
        var names = new List<string>();
        foreach (DataTableRow row in table.Rows)
        {
            names.Add(row["Name"]);
        }

        return names;
    }
}
