using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TempRl
{
    /// <summary>
    /// Loads templates from disk, caching them so they are only fetched once
    /// </summary>
    public class TemplateLoader
    {
        static string _templateFolder = @"Templates\";
        static Dictionary<string, Template> _templateCache = new Dictionary<string, Template>();

        public static Template GetNewTemplate(string name)
        {
            Template ret;
            if (_templateCache.TryGetValue(name, out ret))
                return ret.Clone();

            ret = new Template();
            ret.LoadFromDisk(_templateFolder + name);
            _templateCache.Add(name, ret);
            return ret.Clone();
        }
    }
}
