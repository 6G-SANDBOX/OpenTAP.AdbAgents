// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2020 Universidad de Málaga (University of Málaga), Spain

using System.Collections.Generic;
using System.Linq;

using OpenTap;

namespace Tap.Plugins.UMA.Extensions
{
    public static class ResultTableExtensions
    {
        public static void PublishToSource(this ResultTable resultTable, ResultSource source)
        {
            string name = resultTable.Name;
            List<string> columnNames = resultTable.Columns.Select(c => c.Name).ToList();

            source.PublishTable(name, columnNames, resultTable.Columns.Select(c => c.Data).ToArray());
        }
    }
}
