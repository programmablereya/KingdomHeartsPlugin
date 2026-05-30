using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using DalaMock.Host.Hosting;
using KingdomHeartsPlugin.Configuration;
using KingdomHeartsPlugin.UIElements.Experience;
using Lumina.Excel.Sheets;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KingdomHeartsPlugin
{
    public sealed class KingdomHeartsPlugin : HostedPlugin
    {
        private const string SettingsCommand = "/khpconfig";
        private const string ToggleCommand = "/khp";

        public static string TemplateLocation = "";

        public KingdomHeartsPlugin(
            IDalamudPluginInterface pluginInterface,
            IFramework framework,
            ICommandManager commandManager,
            IClientState clientState,
            IGameGui gameGui,
            IDataManager dataManager,
            ITextureProvider textureProvider,
            IPluginLog pluginLog,
            IObjectTable objectTable
            ) : base(pluginInterface)
        {
            Pi = pluginInterface;
            Fw = framework;
            Cm = commandManager;
            Cs = clientState;
            Gui = gameGui;
            Dm = dataManager;
            Tp = textureProvider;
            Pl = pluginLog;
            Ot = objectTable;
        }

        public override HostedPluginOptions ConfigureOptions()
        {
            var options = new HostedPluginOptions
            {
                UseMediatorService = false
            };
            return options;
        }

        public override async Task StartingAsync(CancellationToken cancellationToken)
        {
            await base.StartingAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Timer = Stopwatch.StartNew();
            TemplateLocation = Path.GetDirectoryName(Pi.AssemblyLocation.FullName!) ?? "";

            var configuration = Pi.GetPluginConfig() as Settings ?? new Settings();
            configuration.Initialize(Pi, Pl);

            Ui = new PluginUI(configuration);
            Portrait.SetAllPortraits();
            cancellationToken.ThrowIfCancellationRequested();
            Cm.AddHandler(SettingsCommand, new CommandInfo(OnSettingsCommand)
            {
                HelpMessage = "Opens configuration for Kingdom Hearts UI Bars."
            });

            Cm.AddHandler(ToggleCommand, new CommandInfo(OnToggleCommand)
            {
                HelpMessage = "Toggles the KH UI."
            });
            cancellationToken.ThrowIfCancellationRequested();
            Fw.Update += OnUpdate;
            Pi.UiBuilder.Draw += DrawUi;
            Pi.UiBuilder.OpenMainUi += ToggleMainVisibility;
            Pi.UiBuilder.OpenConfigUi += DrawConfigUi;
            Cs.TerritoryChanged += OnTerritoryChange;
        }

        public override async Task StoppingAsync()
        {
            Ui?.Dispose();
            
            Cm.RemoveHandler(SettingsCommand);
            Cm.RemoveHandler(ToggleCommand);

            Fw.Update -= OnUpdate;

            Pi.UiBuilder.Draw -= DrawUi;
            Pi.UiBuilder.OpenMainUi -= ToggleMainVisibility;
            Pi.UiBuilder.OpenConfigUi -= DrawConfigUi;
            Cs.TerritoryChanged -= OnTerritoryChange;

            Timer = null;
            await base.StoppingAsync();
        }

        public override void ConfigureContainer(ContainerBuilder containerBuilder)
        {
        }

        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
        }

        private void OnUpdate(IFramework framework)
        {
            if (Timer is null) return;

            UiSpeed = Timer.ElapsedMilliseconds / 1000f;
            Timer.Restart();
            Ui.OnUpdate();
        }

        private void ToggleMainVisibility()
        {
            Ui.Configuration.Enabled = !Ui.Configuration.Enabled;
        }

        private void OnSettingsCommand(string command, string args)
        {
            DrawConfigUi();
        }
        private void OnToggleCommand(string command, string args)
        {
            Ui.Configuration.Enabled = !Ui.Configuration.Enabled;
            Ui.Configuration.Save();
        }

        private void OnTerritoryChange(uint e)
        {
            IsInPvp = GetTerritoryPvP(e);
        }

        private void DrawUi()
        {
            Ui.Draw();
        }

        private void DrawConfigUi()
        {
            Ui.SettingsVisible = true;
        }

        private bool GetTerritoryPvP(uint territoryType)
        {
            try
            {
                var territory = Dm.GetExcelSheet<TerritoryType>().GetRow(territoryType);
                return territory.IsPvpZone;
            }
            catch (KeyNotFoundException)
            {
                Pl.Warning("Could not get territory for current zone");
                return false;
            }
        }
        public static CultureInfo GetCulture()
        {
            try
            {
                return CultureInfo.GetCultureInfo(Ui.Configuration.TextFormatCulture);
            }
            catch
            {
                return CultureInfo.GetCultureInfo("en-US");
            }
        }

        public static IDalamudPluginInterface Pi { get; private set; } = null!;
        public static IFramework Fw { get; private set; } = null!;
        public static ICommandManager Cm { get; private set; } = null!;
        public static IClientState Cs { get; private set; } = null!;
        public static IGameGui Gui { get; private set; } = null!;
        public static IDataManager Dm { get; private set; } = null!;
        public static IPluginLog Pl { get; private set; } = null!;
        public static ITextureProvider Tp { get; private set; } = null!;
        public static IObjectTable Ot { get; private set; } = null!;
        public static PluginUI Ui { get; private set; } = null!;

        public static Stopwatch? Timer { get; private set; }
        public static float UiSpeed { get; set; }
        public static bool IsInPvp { get; private set; }

    }
}
