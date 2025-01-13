using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using ImGuiNET;
using Newtonsoft.Json;
using System.Numerics;

namespace PickIt;

public class PickItSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(false);
    public ToggleNode ShowInventoryView { get; set; } = new(true);
    public RangeNode<Vector2> InventoryPos { get; set; } = new(new Vector2(0, 0), Vector2.Zero, new Vector2(4000, 4000));
    public HotkeyNode ProfilerHotkey { get; set; } = Keys.None;
    public HotkeyNode PickUpKey { get; set; } = Keys.F;
    public ToggleNode PickUpWhenInventoryIsFull { get; set; } = new(false);
    public ToggleNode PickUpEverything { get; set; } = new(false);
    public RangeNode<int> ItemPickupRange { get; set; } = new(600, 1, 1000);
    public RangeNode<int> MonsterCheckRange { get; set; } = new(1000, 1, 2500);

    [Menu(null, "In milliseconds")]
    public RangeNode<int> PauseBetweenClicks { get; set; } = new(100, 0, 500);

    public ToggleNode IgnoreMoving { get; set; } = new(false);

    [ConditionalDisplay(nameof(IgnoreMoving), true)]
    public RangeNode<int> ItemDistanceToIgnoreMoving { get; set; } = new(20, 0, 1000);

    [Menu(null, "Auto pick up any hovered items that match configured filters")]
    public ToggleNode AutoClickHoveredLootInRange { get; set; } = new(false);

    public ToggleNode SmoothCursorMovement { get; set; } = new(true);
    public ToggleNode UseInputLock { get; set; } = new(true);

    public ToggleNode LazyLooting { get; set; } = new(false);

    [ConditionalDisplay(nameof(LazyLooting), true)]
    public ToggleNode NoLazyLootingWhileEnemyClose { get; set; } = new(true);

    [ConditionalDisplay(nameof(LazyLooting), true)]
    public HotkeyNode LazyLootingPauseKey { get; set; } = new(Keys.None);

    [Menu(null, "Includes lazy looting as well as manual activation")]
    public ToggleNode NoLootingWhileEnemyClose { get; set; } = new(false);

    public MiscClickableOptions MiscOptions { get; set; } = new();

    [JsonIgnore]
    public TextNode FilterTest { get; set; } = new();

    [JsonIgnore]
    public ButtonNode ReloadFilters { get; set; } = new();

    [Menu("Use a Custom \"\\config\\custom_folder\" folder ")]
    public TextNode CustomConfigDir { get; set; } = new();

    public List<PickitRule> PickitRules = [];

    [JsonIgnore]
    public FilterNode Filters { get; } = new();

    [Menu(null, "For debugging. Highlights items if they match an existing filter")]
    [JsonIgnore]
    public ToggleNode DebugHighlight { get; set; } = new(false);
}

[Submenu(CollapsedByDefault = true)]
public class MiscClickableOptions
{
    public RangeNode<int> MiscPickitRange { get; set; } = new(15, 0, 600);
    public ToggleNode ClickChests { get; set; } = new(true);
    public ToggleNode ClickDoors { get; set; } = new(true);
    public ToggleNode ClickZoneTransitions { get; set; } = new(false);
}

[Submenu(RenderMethod = nameof(Render))]
public class FilterNode
{
    public void Render(PickIt pickit)
    {
        bool anyChanges = false;
        if (ImGui.Button("Open filter Folder"))
        {
            var configDir = pickit.ConfigDirectory;
            var customConfigFileDirectory = !string.IsNullOrEmpty(pickit.Settings.CustomConfigDir)
                ? Path.Combine(Path.GetDirectoryName(pickit.ConfigDirectory), pickit.Settings.CustomConfigDir)
                : null;

            var directoryToOpen = Directory.Exists(customConfigFileDirectory)
                ? customConfigFileDirectory
                : configDir;

            Process.Start("explorer.exe", directoryToOpen);
        }

        ImGui.Separator();
        ImGui.BulletText("Select Rules To Load");
        ImGui.BulletText("Ordering rule sets so general items will match first rather than last will improve performance");

        var tempNpcInvRules = new List<PickitRule>(pickit.Settings.PickitRules); // Create a copy

        for (int i = 0; i < tempNpcInvRules.Count; i++)
        {
            ImGui.PushID(i);
            if (ImGui.ArrowButton("##upButton", ImGuiDir.Up) && i > 0)
            {
                (tempNpcInvRules[i - 1], tempNpcInvRules[i]) = (tempNpcInvRules[i], tempNpcInvRules[i - 1]);
                anyChanges = true;
            }

            ImGui.SameLine();
            ImGui.Text(" ");
            ImGui.SameLine();

            if (ImGui.ArrowButton("##downButton", ImGuiDir.Down) && i < tempNpcInvRules.Count - 1)
            {
                (tempNpcInvRules[i + 1], tempNpcInvRules[i]) = (tempNpcInvRules[i], tempNpcInvRules[i + 1]);
                anyChanges = true;
            }

            ImGui.SameLine();
            ImGui.Text(" - ");
            ImGui.SameLine();

            if (ImGui.Checkbox($"{tempNpcInvRules[i].Name}###enabled", ref tempNpcInvRules[i].Enabled))
            {
                anyChanges = true;
            }
            ImGui.PopID();
        }

        pickit.Settings.PickitRules = tempNpcInvRules;
        if (anyChanges)
        {
            pickit.LoadRuleFiles();
        }
    }
}

public record PickitRule(string Name, string Location, bool Enabled)
{
    public bool Enabled = Enabled;
}