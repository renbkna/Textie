using System;
using System.Threading.Tasks;
using Spectre.Console;
using Textie.Core.Configuration;
using Textie.Core.UI;
using Textie.Core.Input;
using Textie.Core.Spammer;

namespace Textie.Core
{
    public class TextieApplication : IDisposable
    {
        private readonly ConfigurationManager _configManager;
        private readonly UserInterface _ui;
        private readonly GlobalKeyboardHook _keyboardHook;
        private readonly TextSpammerEngine _spammerEngine;
        private bool _disposed = false;

        public TextieApplication()
        {
            _configManager = new ConfigurationManager();
            _ui = new UserInterface();
            _keyboardHook = new GlobalKeyboardHook();
            _spammerEngine = new TextSpammerEngine();

            // Wire up events
            _keyboardHook.EnterPressed += OnEnterPressed;
            _keyboardHook.EscapePressed += OnEscapePressed;
            _spammerEngine.SpamStarted += OnSpamStarted;
            _spammerEngine.SpamCompleted += OnSpamCompleted;
            _spammerEngine.SpamCancelled += OnSpamCancelled;
        }

        public async Task RunAsync()
        {
            try
            {
                _ui.ShowWelcome();

                var mainLoop = true;
                var needsConfiguration = true;

                while (mainLoop)
                {
                    if (needsConfiguration)
                    {
                        var config = await _ui.CollectConfigurationAsync(_configManager.GetConfiguration());

                        // Check if configuration was cancelled (empty config returned)
                        if (string.IsNullOrEmpty(config.Message))
                        {
                            continue; // Stay in configuration mode
                        }

                        _configManager.UpdateConfiguration(config);
                        needsConfiguration = false;
                    }

                    _ui.ShowInstructions(_configManager.GetConfiguration());
                    _ui.ShowWaitingStatus();

                    // Setup keyboard hook and wait for user action
                    if (!_keyboardHook.Install())
                    {
                        _ui.ShowError("Failed to install keyboard hook. Try running as administrator.");
                        break;
                    }

                    // Wait for hook events to trigger spam operation or exit
                    await _keyboardHook.WaitForNextActionAsync();

                    // Cleanup hook before showing next options
                    _keyboardHook.Uninstall();

                    // Show completion status and get next action
                    var nextAction = _ui.GetNextAction();

                    switch (nextAction)
                    {
                        case NextAction.RunAgain:
                            needsConfiguration = false;
                            break;
                        case NextAction.ChangeSettings:
                            needsConfiguration = true;
                            break;
                        case NextAction.Exit:
                            mainLoop = false;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _ui.ShowError($"Application error: {ex.Message}");
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes);
            }
            finally
            {
                _ui.ShowGoodbye();
            }
        }

        private async void OnEnterPressed()
        {
            if (!_spammerEngine.IsSpamming)
            {
                var config = _configManager.GetConfiguration();
                await _spammerEngine.StartSpammingAsync(config);
            }
        }

        private void OnEscapePressed()
        {
            if (_spammerEngine.IsSpamming)
            {
                _spammerEngine.StopSpamming();
            }
        }

        private void OnSpamStarted()
        {
            _ui.ShowSpamStarted();
        }

        private void OnSpamCompleted()
        {
            _ui.ShowSpamCompleted();
            _keyboardHook.SignalCompletion();
        }

        private void OnSpamCancelled()
        {
            _ui.ShowSpamCancelled();
            _keyboardHook.SignalCompletion();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _keyboardHook?.Dispose();
                _spammerEngine?.Dispose();
                _disposed = true;
            }
        }
    }
}
