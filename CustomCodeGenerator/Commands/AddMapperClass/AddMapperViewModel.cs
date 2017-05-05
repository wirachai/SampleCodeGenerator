using System;
using System.Collections.Generic;
using System.Linq;
using CustomCodeGenerator.Helpers;
using CustomCodeGenerator.Models;
using EnvDTE;

namespace CustomCodeGenerator.Commands.AddMapperClass
{
    public class AddMapperViewModel
    {
        public IEnumerable<ModelType> ModelTypes { get; set; }

        public AddMapperViewModel(Project project)
        {
            ModelTypes = GetAllModelTypes(project);
        }

        public IEnumerable<ModelType> SourceModelTypes => ModelTypes;

        public ModelType SelectedSourceModelType { get; set; }

        public IEnumerable<ModelType> DestinationModelTypes => ModelTypes;

        public ModelType SelectedDestinationModelType { get; set; }

        private IEnumerable<ModelType> GetAllModelTypes(Project project)
        {
            return project.GetCodeTypes().Select(x => new ModelType(x));
        }
    }
}