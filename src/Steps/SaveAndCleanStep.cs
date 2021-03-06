﻿using System.IO;

namespace SharpDox.Plugins.Chm.Steps
{
    internal class SaveAndCleanStep : StepBase
    {
        public SaveAndCleanStep(int progressStart, int progressEnd) : base(new StepRange(progressStart, progressEnd)) { }

        public override void RunStep()
        {
            SaveTo(StepInput.OutputPath, 0);
            CleanUp();
        }

        private void SaveTo(string path, int appenderNumber)
        {
            try
            {
                ExecuteOnStepMessage(StepInput.ChmStrings.Saving);

                var copyTo = Path.Combine(path, $"{StepInput.SDProject.ProjectName}-{StepInput.CurrentTargetFx.Name}-{StepInput.CurrentLanguage}.chm");
                if (appenderNumber > 0)
                {
                    copyTo = Path.Combine(path, $"{StepInput.SDProject.ProjectName}{appenderNumber}-{StepInput.CurrentTargetFx.Name}-{StepInput.CurrentLanguage}.chm");
                }

                File.Copy(Path.Combine(StepInput.TmpPath, StepInput.SDProject.ProjectName.Replace(" ", "") + ".chm"), copyTo, true);
            }
            catch (IOException)
            {
                SaveTo(path, appenderNumber + 1);
            }
        }

        private void CleanUp()
        {
            ExecuteOnStepMessage(StepInput.ChmStrings.Cleaning);
            Directory.Delete(StepInput.TmpPath, true);
        }
    }
}
