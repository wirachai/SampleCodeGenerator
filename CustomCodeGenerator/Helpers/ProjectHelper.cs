using EnvDTE;
using System.Collections.Generic;
using System.Linq;

namespace CustomCodeGenerator.Helpers
{
    public static class ProjectHelper
    {
        public static List<string> GetNameSpaces(this Project project)
        {
            var namespaces = new List<string>();
            foreach (EnvDTE.ProjectItem item in GetProjectItemsRecursively(project.ProjectItems))
            {
                if (item.FileCodeModel == null) continue;
                foreach (EnvDTE.CodeElement elem in item.FileCodeModel.CodeElements)
                {
                    if (elem.Kind == EnvDTE.vsCMElement.vsCMElementNamespace
                        && !namespaces.Contains(elem.FullName))
                        namespaces.Add(elem.FullName);
                }
            }
            return namespaces;
        }

        public static IEnumerable<CodeNamespace> GetCodeNamespaces(this Project project, List<string> namespaces)
        {
            return project
                .CodeModel
                .CodeElements
                .OfType<EnvDTE.CodeNamespace>()
                .Where(cn => namespaces.Contains(cn.FullName));
        }

        private static List<CodeType> GetCodeTypes(IEnumerable<CodeNamespace> codeNamespaces)
        {
            var codeTypes = new List<CodeType>();
            foreach (EnvDTE.CodeNamespace ns in codeNamespaces)
            {
                foreach (EnvDTE.CodeType ct in ns.Members.OfType<EnvDTE.CodeType>())
                {
                    codeTypes.Add(ct);
                }
            }
            return codeTypes;
        }

        public static List<CodeType> GetCodeTypes(this Project project)
        {
            var namespaces = GetNameSpaces(project);
            var codeNamespaces = GetCodeNamespaces(project, namespaces);
            return GetCodeTypes(codeNamespaces);
        }

        private static List<EnvDTE.ProjectItem> GetProjectItemsRecursively(EnvDTE.ProjectItems items)
        {
            var ret = new List<EnvDTE.ProjectItem>();
            if (items == null) return ret;
            foreach (EnvDTE.ProjectItem item in items)
            {
                ret.Add(item);
                ret.AddRange(GetProjectItemsRecursively(item.ProjectItems));
            }
            return ret;
        }
    }

}