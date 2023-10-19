using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Cairo;

using TeaLib.GuiExtensions;

namespace TeaLib
{
	namespace TeaConfig
	{
		public enum EnumTeaConfigDialogSettingSide
		{
			Client = 0,
			Server = 1
		}

		public enum EnumTeaConfigDialogSettingType
		{
			Global = 0,
			World = 1
		}

		public class TeaConfigDialog : GuiDialog
		{
			private string DialogName {get;} = "tealib.modconfigdialog";

			public override string ToggleKeyCombinationCode => null;
			public override bool PrefersUngrabbedMouse => false;

			private readonly (EnumTeaConfigDialogSettingSide Type, string Code, string Name)[] _settingSideData;

			private readonly GuiTab[] _sideSelectTabs;
			private (string code, string name)[] _optionCategories = new (string, string)[0];

			private List<TeaConfigModSettings> _modSettingsList;

			private EnumTeaConfigDialogSettingType _selectedSettingType = EnumTeaConfigDialogSettingType.Global;
			private EnumTeaConfigDialogSettingSide _selectedSettingSide = EnumTeaConfigDialogSettingSide.Client;

			private string _selectedConfigId;
			private int _selectedOptionGroupIndex;

			private readonly Dictionary<TeaConfigDialogChangedSetting, object> changedSettings = new();

			public TeaConfigDialog(ICoreClientAPI capi) : base(capi) 
			{
				_settingSideData = new[] {
					(EnumTeaConfigDialogSettingSide.Client, "client", Lang.Get("tealib:client")),
					(EnumTeaConfigDialogSettingSide.Server, "server", Lang.Get("tealib:server")),
				};

				_sideSelectTabs = new GuiTab[1]
				{
					new GuiTab { Name = Lang.Get("tealib:global-settings"), DataInt = (int)EnumTeaConfigDialogSettingType.Global },
					// Temporarily commented out until world settings can be implemented
					//new GuiTab { Name = Lang.Get("tealib:world-settings"), DataInt = (int)EnumTeaConfigDialogSettingType.World }
				};
			}

			public static TeaConfigDialog GetLoaded(ICoreClientAPI capi)
			{
				return capi.Gui.LoadedGuis.Find((dialog) => dialog is TeaConfigDialog) as TeaConfigDialog;
			}

			public void OpenWithData(IEnumerable<TeaConfigModSettings> modSettings)
			{
				_modSettingsList = modSettings.OrderBy(modSettings => modSettings.ConfigName).ToList();

				if (_modSettingsList.Count > 0) TrySetValidConfigId();

				TryOpen();
			}

			public override void OnGuiOpened()
			{
				ComposeDialog();
			}

			public void ComposeDialog(bool clear = false)
			{
				// Auto-sized dialog at the center of the screen
				ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithFixedOffset(0, 0);

				if (clear) SingleComposer.Clear(dialogBounds);

				int containerWidth = 400;
				int containerHeight = 600;
				int containerMargin = 16;

				// Background boundaries
				ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding).WithSizing(ElementSizing.FitToChildren);

				// Module area
				// TODO: Move the bounds to a separate method, and cache them in a dictionary?
				ElementBounds fullBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, containerWidth + containerMargin, 0)
				.WithSizing(ElementSizing.Fixed, ElementSizing.FitToChildren)
				.WithParentMutual(bgBounds);

				ElementBounds tabsBounds = ElementBounds.Percentual(EnumDialogArea.CenterTop, 1, 0)
				.WithSizing(ElementSizing.Percentual, ElementSizing.Fixed)
				.WithFixedHeight(48)
				.WithParentMutual(fullBounds);

				ElementBounds sideSelectBounds = tabsBounds.BelowCopy(0, 16).WithFixedHeight(32).WithParentMutual(fullBounds);
				ElementBounds sideSelectLabelBounds = ElementBounds.Percentual(EnumDialogArea.LeftMiddle, 0.3, 1).WithParentMutual(sideSelectBounds);
				ElementBounds sideSelectDropDownBounds = ElementBounds.Percentual(EnumDialogArea.RightMiddle, 0.7, 1).WithParentMutual(sideSelectBounds);

				ElementBounds horizontalLineBounds = sideSelectBounds.BelowCopy(0, 16).WithFixedHeight(16).WithParentMutual(fullBounds);

				ElementBounds modSearchBounds = horizontalLineBounds.BelowCopy(0, 16).WithFixedHeight(32).WithParentMutual(fullBounds);
				ElementBounds modSearchLabelBounds = ElementBounds.Percentual(EnumDialogArea.LeftMiddle, 0.4, 1).WithParentMutual(modSearchBounds);
				ElementBounds modSearchInputBounds = ElementBounds.Percentual(EnumDialogArea.RightMiddle, 0.6, 1).WithParentMutual(modSearchBounds);

				ElementBounds modSelectBounds = modSearchBounds.BelowCopy(0, 16).WithFixedHeight(32).WithParentMutual(fullBounds);
				ElementBounds modSelectLabelBounds = ElementBounds.Percentual(EnumDialogArea.LeftMiddle, 0.3, 1).WithParentMutual(modSelectBounds);
				ElementBounds modSelectDropDownBounds = ElementBounds.Percentual(EnumDialogArea.RightMiddle, 0.7, 1).WithParentMutual(modSelectBounds);

				// TODO: add inset?
				ElementBounds optionGroupsBounds = modSelectBounds.BelowCopy(0, 16).WithFixedHeight(32).WithParentMutual(fullBounds);
				ElementBounds optionsAreaBounds = optionGroupsBounds.BelowCopy().WithFixedHeight(400).WithParentMutual(fullBounds);

				ElementBounds buttonRowBounds = optionsAreaBounds.BelowCopy(0, 16).WithFixedHeight(32).WithParentMutual(fullBounds);
				// TODO: Percentual button width currently crashes the game. Can be patched now that .NET 7 is out
				//ElementBounds buttonRowSaveBounds = ElementBounds.Percentual(EnumDialogArea.LeftMiddle, 0.2, 1).WithParentMutual(buttonRowBounds);
				//ElementBounds buttonRowCancelBounds = ElementBounds.Percentual(EnumDialogArea.RightMiddle, 0.2, 1).WithParentMutual(buttonRowBounds);
				ElementBounds buttonRowCancelBounds = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 0, 0, 96, 32).WithParentMutual(buttonRowBounds);
				ElementBounds buttonRowSaveBounds = ElementBounds.Fixed(EnumDialogArea.RightMiddle, 0, 0, 64, 32).WithParentMutual(buttonRowBounds);

				List<TeaConfigModSettings> availableModSettings = GetAvailableModSettings(_selectedSettingSide).ToList();

				string[] configIDs = availableModSettings.Select(modSettings => modSettings.ConfigID).ToArray();
				string[] configNames = availableModSettings.Select(modSettings => modSettings.ConfigName).ToArray();
				
				(string[] settingSideCodes, string[] settingSideNames) = GetSideCodesAndNames();

				int selectedSideOption = (int)_selectedSettingSide;
				int selectedMod = availableModSettings.FindIndex(mod => mod.ConfigID == _selectedConfigId);

				RefreshOptionTabGroups();
				GuiTab[] optionGroupTabs = GetOptionGroupTabs();

				SingleComposer = capi.Gui.CreateCompo(DialogName, dialogBounds)
					.AddShadedDialogBG(bgBounds, true)
					.AddDialogTitleBar(Lang.Get("tealib:mod-configuration"), OnTitleBarClose)
					.AddHorizontalTabs(_sideSelectTabs, tabsBounds, (tabNum) => { }, CairoFont.WhiteSmallText(), CairoFont.WhiteSmallText().WithColor(GuiStyle.ActiveButtonTextColor))
					.AddStaticText(Lang.Get("tealib:settings-type"), CairoFont.WhiteSmallText(), sideSelectLabelBounds)
					.AddDropDownFixed(settingSideCodes, settingSideNames, selectedSideOption, OnSideSelectChanged, sideSelectDropDownBounds)
					.AddHorizontalLine(horizontalLineBounds)
					.AddStaticText(Lang.Get("tealib:search-mod-by-name-label"), CairoFont.WhiteSmallText(), modSearchLabelBounds)
					.AddTextInput(modSearchInputBounds, OnModSearchInputChanged, null, "modSearchInput")
					.AddStaticText(Lang.Get("tealib:mod-label"), CairoFont.WhiteSmallText(), modSelectLabelBounds)
					.AddDropDownFixed(configIDs, configNames, selectedMod, OnModSelectChanged, modSelectDropDownBounds)
					.AddHorizontalTabs(optionGroupTabs, optionGroupsBounds, OnOptionGroupTabChanged, CairoFont.WhiteSmallText(), CairoFont.WhiteSmallText().WithColor(GuiStyle.ActiveButtonTextColor), "optionGroupTabs")
					.AddScrollableArea(optionsAreaBounds, ComposeModOptions, "optionsArea")
					.AddButton(Lang.Get("tealib:button-save"), SaveAndExit, buttonRowSaveBounds)
					.AddButton(Lang.Get("tealib:button-cancel"), TryClose, buttonRowCancelBounds)
					.Compose()
				;

				SingleComposer.GetHorizontalTabs("optionGroupTabs").activeElement = _selectedOptionGroupIndex;

				SingleComposer.GetTextInput("modSearchInput").SetPlaceHolderText(Lang.Get("tealib:not-implemented"));

				// Would have been neat to be able to automate this, but alas there's no GuiElement method the GuiComposer would call after finishing composition 
				SingleComposer.GetScrollableArea("optionsArea").CalcTotalHeight();
			}

			public void TrySetValidConfigId()
			{
				// If a valid config is already set, we don't need to do anything
				if (GetCurrentModSettings() != null) return;

				IEnumerable<TeaConfigModSettings> availableModSettings = GetAvailableModSettings(_selectedSettingSide);
				TeaConfigModSettings firstModSettings = availableModSettings.FirstOrDefault();

				_selectedConfigId = firstModSettings?.ConfigID;
			}

			public (string[], string[]) GetSideCodesAndNames()
			{
				List<string> sideSelectCodes = new();
				List<string> sideSelectNames = new();

				foreach ((EnumTeaConfigDialogSettingSide Type, string Code, string Name) settingSide in _settingSideData)
				{
					sideSelectCodes.Add(settingSide.Code);
					sideSelectNames.Add(settingSide.Name);
				}

				return (sideSelectCodes.ToArray(), sideSelectNames.ToArray());
			}

			private void RefreshOptionTabGroups()
			{
				TeaConfigModSettings modSettings = GetCurrentModSettings();

				if (modSettings == null) {
					_optionCategories = new (string, string)[0];
					return;
				}

				ReadOnlyCollection<TeaConfigSetting> settingsArray = _selectedSettingSide switch
				{
					EnumTeaConfigDialogSettingSide.Client => modSettings.ClientSettings,
					EnumTeaConfigDialogSettingSide.Server => modSettings.ServerSettings,
					_ => throw new ArgumentException("Unknown mod setting side")
				};

				if (settingsArray == null) {
					_optionCategories = new (string, string)[0];
					return;
				}

				_optionCategories = settingsArray
					.GroupBy(setting => setting.Category)
					.Select(group => 
					{
						string category = group.FirstOrDefault().Category;
						string langKey = GetLangKeyWithBackup(modSettings.ConfigID, $"modconfig-category-{category}");
						return (category, langKey);
					})
					.ToArray();
			}

			// Attempts to return either a translated string from the supplied domain, or a backup from the 'tealib' domain 
			// Looks at current locale first, then at default locale, and finally just returns the key if nothing is found
			private string GetLangKeyWithBackup(string domain, string key)
			{
				if (Lang.HasTranslation($"{domain}:{key}")) return Lang.Get($"{domain}:{key}");
				if (Lang.HasTranslation($"tealib:{key}")) return Lang.Get($"tealib:{key}");

				ITranslationService defaultLang = Lang.AvailableLanguages[Lang.DefaultLocale];

				if (defaultLang.HasTranslation($"{domain}:{key}")) return defaultLang.Get($"{domain}:{key}");
				if (defaultLang.HasTranslation($"tealib:{key}")) return defaultLang.Get($"tealib:{key}");

				return $"{domain}:{key}";
			}

			public IEnumerable<TeaConfigModSettings> GetAvailableModSettings(EnumTeaConfigDialogSettingSide settingSide)
			{
				IEnumerable<TeaConfigModSettings> availableConfigs = settingSide switch
				{
					EnumTeaConfigDialogSettingSide.Client => _modSettingsList.Where(modSettings => modSettings.ClientSettings != null),
					EnumTeaConfigDialogSettingSide.Server => _modSettingsList.Where(modSettings => modSettings.ServerSettings != null),
					_ => throw new ArgumentException("Unknown mod setting side")
				};

				// TODO: Add filtering by mod search input as well
				return availableConfigs;
			}

			private GuiTab[] GetOptionGroupTabs()
			{
				GuiTab[] optionGroupTabs = _optionCategories.Select((category, index) => new GuiTab
				{
					Name = category.name,
					DataInt = index
				})
				.ToArray();

				return optionGroupTabs;
			}

			private TeaConfigModSettings GetCurrentModSettings()
			{
				return GetAvailableModSettings(_selectedSettingSide).Where(modSettings => modSettings.ConfigID == _selectedConfigId).FirstOrDefault();
			}

			private (string, List<TeaConfigSetting>) GetCurrentSettingList()
			{
				TeaConfigModSettings currentModSettings = GetCurrentModSettings();

				if (currentModSettings == null || _optionCategories.Length == 0) return (null, null);

				string currentCategory = _optionCategories[_selectedOptionGroupIndex].code;

				(string configId, List<TeaConfigSetting> settingList) = _selectedSettingSide switch
				{
					EnumTeaConfigDialogSettingSide.Client => (currentModSettings.ConfigID, currentModSettings.ClientSettings.Where(setting => setting.Category == currentCategory).ToList()),
					EnumTeaConfigDialogSettingSide.Server => (currentModSettings.ConfigID, currentModSettings.ServerSettings.Where(setting => setting.Category == currentCategory).ToList()),
					_ => throw new ArgumentException("Unknown mod setting side")
				};

				return (configId, settingList);
			}

			private void ComposeModOptions(GuiElementContainer container, ElementBounds bounds)
			{
				(string configId, List<TeaConfigSetting> settingList) = GetCurrentSettingList();

				if (settingList == null) return;

				ElementBounds currentBounds = null;
				ElementBounds lastBounds = null;

				foreach (TeaConfigSetting setting in settingList)
				{
					currentBounds = ElementBounds.Percentual(EnumDialogArea.CenterMiddle, 1, 1).WithVerticalSizing(ElementSizing.Fixed).WithFixedHeight(32).WithParentMutual(bounds);

					TeaConfigDialogChangedSetting changedSettingId = new(configId, _selectedSettingSide, setting);
					object inputValue = changedSettings.GetValueOrDefault(changedSettingId);

					ElementBounds inputBounds = ElementBounds.Percentual(EnumDialogArea.RightTop, 0.4, 1).WithParentMutual(currentBounds);

					string settingLangKey = $"{configId}:modconfig-setting-{setting.Code.ToLowerInvariant()}";

					GuiElement settingInput;

					if (!(setting.Config.ConfigType == EnumTeaConfigApiSide.Server && !capi.World.Player.HasPrivilege(Privilege.controlserver)))
					{
						settingInput = setting.GetInputElement(capi, inputBounds, inputValue, "Placeholder", (input) =>
						{
							changedSettings[changedSettingId] = input;
						}, settingLangKey);
					}
					else
					{
						settingInput = new GuiElementStaticText(capi, setting.GetAsString(), EnumTextOrientation.Left, inputBounds, CairoFont.WhiteSmallText());
					}

					if (settingInput == null) continue;

					container.Add(settingInput);

					ElementBounds labelBounds = ElementBounds.Percentual(EnumDialogArea.LeftTop, 0.6, 1).WithParentMutual(currentBounds);
					GuiElementStaticText labelText = new(capi, Lang.Get(settingLangKey), EnumTextOrientation.Left, labelBounds, CairoFont.WhiteSmallText());
					container.Add(labelText);

					if (lastBounds != null) currentBounds.FixedUnder(lastBounds, 10);
					lastBounds = currentBounds;
				}
			}

			private void OnSideSelectChanged(string code, bool selected)
			{
				if (!Enum.TryParse(code, true, out EnumTeaConfigDialogSettingSide side)) return;
				if (side == _selectedSettingSide) return;

				_selectedSettingSide = side;
				_selectedOptionGroupIndex = 0;

				TrySetValidConfigId();

				ComposeDialog();
			}

			private void OnModSearchInputChanged(string newText)
			{
				// This one might be a bit more difficult - we'll want to reduce the choice in the mod dropdown based on the search, but not actually automatically 
				// select a new mod if the currently selected one is not in the dropdown anymore 
				//capi.ShowChatMessage($"Mod search input changed to {newText}");
			}

			private void OnModSelectChanged(string code, bool selected)
			{
				if (_selectedConfigId == code) return;

				_selectedConfigId = code;
				_selectedOptionGroupIndex = 0;

				ComposeDialog();
			}

			private void OnOptionGroupTabChanged(int tab)
			{
				if (_selectedOptionGroupIndex == tab) return;

				_selectedOptionGroupIndex = tab;

				ComposeDialog();
			}

			private void SaveConfigSettings()
			{
				TreeAttribute dataTree = new();
				TreeAttribute configListTree = new();
				dataTree.SetAttribute("configs", configListTree);

				foreach (KeyValuePair<TeaConfigDialogChangedSetting, object> keyValuePair in changedSettings)
				{
					string configID = keyValuePair.Key.ConfigID;
					if (!configListTree.HasAttribute(configID)) configListTree.SetAttribute(configID, new TreeAttribute());
					
					TreeAttribute configSettingsTree = configListTree.GetTreeAttribute(configID) as TreeAttribute;

					string sideName = keyValuePair.Key.SettingSide switch
					{
						EnumTeaConfigDialogSettingSide.Client => Enum.GetName(EnumTeaConfigApiSide.Client),
						EnumTeaConfigDialogSettingSide.Server => Enum.GetName(EnumTeaConfigApiSide.Server),
						_ => throw new ArgumentException("Unknown EnumTeaConfigDialogSettingSide")
					};
					if (!configSettingsTree.HasAttribute(sideName)) configSettingsTree.SetAttribute(sideName, new TreeAttribute());

					TreeAttribute settingTree = configSettingsTree.GetTreeAttribute(sideName) as TreeAttribute;

					settingTree.SetString(keyValuePair.Key.Setting.Code, keyValuePair.Key.Setting.ConvertValueToString(keyValuePair.Value));
				}

				capi.ModLoader.GetModSystem<TeaConfigDialogSystem>()?.SendSavedSettings(dataTree);
			}

			private bool SaveAndExit()
			{
				SaveConfigSettings();

				return TryClose();
			}

			public override bool TryClose()
			{
				changedSettings.Clear();

				return base.TryClose();
			}

			private void OnTitleBarClose() => TryClose();
		}
	}
}