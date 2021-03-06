﻿using System;
using SharpDox.Sdk.Exporter;
using SharpDox.Plugins.Chm.Steps;
using System.IO;
using SharpDox.Model;
using System.Collections.Generic;
using SharpDox.Sdk.Local;
using SharpDox.Model.Repository;
using System.Linq;

namespace SharpDox.Plugins.Chm
{
    public class ChmExporter : IExporter
    {
        public event Action<string> OnRequirementsWarning;
        public event Action<string> OnStepMessage;
        public event Action<int> OnStepProgress;

        private double _docCount;
        private double _docIndex;

        private readonly ILocalController _localController;
        private readonly ChmConfig _chmConfig;
        private readonly ChmStrings _chmStrings;

        public ChmExporter(ChmConfig chmConfig, ILocalController localController)
        {
            _chmConfig = chmConfig;
            _localController = localController;
            _chmStrings = localController.GetLocalStrings<ChmStrings>();
        }

        public void Export(SDProject sdProject, string outputPath)
        {
            _docCount = sdProject.DocumentationLanguages.Count;
            _docIndex = 0;

            foreach(var targetFx in sdProject.GetAllAvailableTargetFxs())
            {
                if (ApiEmpty(sdProject, targetFx))
                {
                    continue;
                }

                foreach (var docLanguage in sdProject.DocumentationLanguages)
                {
                    StepInput.InitStepinput(sdProject, targetFx, Path.Combine(outputPath, docLanguage), docLanguage, _localController.GetLocalStringsOrDefault<ChmStrings>(docLanguage), _chmStrings, _chmConfig);

                    var steps = new List<StepBase>();
                    steps.Add(new CopyStep(0, 10));
                    steps.Add(new TemplateStep(10, 50));
                    steps.Add(new CompileStep(50, 90));
                    steps.Add(new SaveAndCleanStep(90, 100));

                    foreach (var step in steps)
                    {
                        ExecuteOnStepProgress(step.StepRange.ProgressStart);

                        step.OnStepMessage += ExecuteOnStepMessage;
                        step.OnStepProgress += ExecuteOnStepProgress;
                        step.RunStep();
                    }

                    _docIndex++;
                }
            }            
        }

        public bool CheckRequirements()
        {
            var requirements = File.Exists(Path.Combine(_chmConfig.CompilerPath, "hhc.exe"));
            if (!requirements)
            {
                ExecuteOnRequirementsWarning(_chmStrings.CompilerNotFound);
            }

            return requirements;
        }

        private bool ApiEmpty(SDProject sdProject, SDTargetFx targetFx)
        {
            var empty = true;
            foreach (var solution in sdProject.Solutions.Values)
            {
                var sdRepository = solution.Repositories.SingleOrDefault(r => r.TargetFx.Identifier == targetFx.Identifier);
                if (sdRepository != null && sdRepository.GetAllNamespaces().Count > 0)
                {
                    empty = false;
                    break;
                }
            }
            return empty;
        }

        internal void ExecuteOnStepMessage(string message)
        {
            var handler = OnStepMessage;
            if (handler != null)
            {
                handler(string.Format("({0}) - {1}", StepInput.CurrentLanguage, message));
            }
        }

        internal void ExecuteOnStepProgress(int progress)
        {
            var handler = OnStepProgress;
            if (handler != null)
            {
                handler((int)((progress / _docCount) + (100 / _docCount * _docIndex)));
            }
        }

        internal void ExecuteOnRequirementsWarning(string message)
        {
            var handler = OnRequirementsWarning;
            if (handler != null)
            {
                handler(message);
            }
        }

        public string ExporterName { get { return "Chm"; } }
    }
}
