using EnvDTE;
using EnvDTE80;
using System;
using System.Diagnostics;

namespace CustomCodeGenerator.Helpers
{
    public static class VisualStudioHelper
    {
        public static Project GetActiveProject(this DTE2 dte)
        {
            try
            {
                var activeSolutionProjects = dte.ActiveSolutionProjects as Array;

                if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
                    return activeSolutionProjects.GetValue(0) as Project;

                var doc = dte.ActiveDocument;

                if (doc != null && !string.IsNullOrEmpty(doc.FullName))
                {
                    var item = (dte.Solution != null) ? dte.Solution.FindProjectItem(doc.FullName) : null;

                    if (item != null)
                        return item.ContainingProject;
                }
            }
            catch (Exception ex)
            {
                Debug.Write("Error getting the active project" + ex);
            }

            return null;
        }
    }
}