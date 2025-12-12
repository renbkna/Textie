using System;
using Textie.Core.Configuration;

namespace Textie.Core.UI.Views;
    public enum WizardNavigation
    {
        Next,
        Back,
        Cancel,
        Complete
    }

    public abstract class WizardStep
    {
        public abstract string Title { get; }
        public abstract string Description { get; }

        public abstract WizardNavigation Execute(SpamConfiguration workingConfig);
    }
