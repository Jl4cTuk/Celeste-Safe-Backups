using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Celeste.Mod.InfiniteBackups.Utils;
using Microsoft.Xna.Framework;
using MonoMod.Utils;

namespace Celeste.Mod.InfiniteBackups {
    [SettingName(DialogId.ModName)]
    public class InfiniteBackupsSettings : EverestModuleSettings {
        public bool Enabled { get; set; } = true;

#if DEBUG
        public bool LogToIngameConsole { get; set; } = true;
#endif

        public bool BackupAsZipFile { get; set; } = false;

        public bool AutoDeleteOldBackups { get; set; } = false;

        public int DeleteBackupsOlderThanDays { get; set; } = -1;

        public int DeleteBackupsAfterAmount { get; set; } = -1;

        private Dictionary<string, TextMenu.Item> menuItems = new Dictionary<string, TextMenu.Item>(StringComparer.OrdinalIgnoreCase);

        public void CreateEnabledEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.OnOff(DialogId.Options.Enabled.DialogClean(), Enabled)
                .Change(value => {
                    Enabled = value;
                    if (Enabled) {
                        InfiniteBackupsModule.Instance.Load();
                    } else {
                        InfiniteBackupsModule.Instance.Unload();
                    }
                    RefreshItemsStates();
                });
            textMenu.Add(item);
            menuItems.Add(DialogId.Options.Enabled, item);
        }

#if DEBUG
        public void CreateLogToIngameConsoleEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.OnOff("Log to ingame console [DEBUG]", LogToIngameConsole)
                .Change(value => {
                    LogToIngameConsole = value;
                });
            textMenu.Add(item);
        }
#endif

        public void CreateBackupAsZipFileEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.OnOff(DialogId.Options.BackupAsZipFile.DialogClean(), BackupAsZipFile)
                .Change(value => {
                    BackupAsZipFile = value;
                });
            textMenu.Add(item);
            menuItems.Add(DialogId.Options.BackupAsZipFile, item);
        }

        public void CreateAutoDeleteOldBackupsEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.OnOff(DialogId.Options.AutoDeleteOldBackups.DialogClean(), AutoDeleteOldBackups)
                .Change(value => {
                    AutoDeleteOldBackups = value;
                    RefreshItemsStates();
                });
            textMenu.Add(item);
            menuItems.Add(DialogId.Options.AutoDeleteOldBackups, item);
        }

        public void CreateDeleteBackupsOlderThanDaysEntry(TextMenu textMenu, bool inGame) {
            BetterIntSlider item = new BetterIntSlider(DialogId.Options.DeleteBackupsOlderThanDays.DialogClean(),
                i => i == -1
                    ? DialogId.OptionValues.Disabled.DialogClean()
                    : string.Format(DialogId.OptionValues.Days.DialogGet(), i),
                -1,
                100,
                DeleteBackupsOlderThanDays);
            item.Change(value => {
                // skip 0 days
                if (value > -1 && value < 1) {
                    item.Index = item.LastDir > 0 ? 1 : -1;
                    value = item.Index;
                }
                DeleteBackupsOlderThanDays = value;
            });
            item.ValueWidthFunc = () => {
                float width = 0;
                width = Math.Max(width, ActiveFont.Measure(item.ValuesFunc(item.Min)).X);
                width = Math.Max(width, ActiveFont.Measure(item.ValuesFunc(item.Max)).X);
                return width;
            };
            textMenu.Add(item);
            menuItems.Add(DialogId.Options.DeleteBackupsOlderThanDays, item);
        }

        public void CreateDeleteBackupsAfterAmountEntry(TextMenu textMenu, bool inGame) {
            BetterIntSlider item = new BetterIntSlider(DialogId.Options.DeleteBackupsAfterAmount.DialogClean(),
                i => i == -1 ? DialogId.OptionValues.Disabled.DialogClean() : $"{i}",
                -1,
                500,
                DeleteBackupsAfterAmount);
            item.Change(value => {
                // skip 0
                if (value > -1 && value < 1) {
                    item.Index = item.LastDir > 0 ? 1 : -1;
                    value = item.Index;
                }
                DeleteBackupsAfterAmount = value;
            });
            item.ValueWidthFunc = () => {
                float width = 0;
                width = Math.Max(width, ActiveFont.Measure(item.ValuesFunc(item.Min)).X);
                width = Math.Max(width, ActiveFont.Measure(item.ValuesFunc(item.Max)).X);
                return width;
            };
            textMenu.Add(item);
            menuItems.Add(DialogId.Options.DeleteBackupsAfterAmount, item);
        }

        public void CreateOpenBackupFolderEntry(TextMenu textMenu, bool inGame) {
            TextMenu.Item item = new TextMenu.Button(DialogId.Options.OpenBackupFolder.DialogClean())
                .Pressed(() => {
                    try {
                        string backupPath = Modules.InfiniteBackups.BackupPath;
                        if (!Directory.Exists(backupPath)) {
                            Directory.CreateDirectory(backupPath);
                        }
                        Process.Start(new ProcessStartInfo(backupPath) {
                            UseShellExecute = true
                        });
                    } catch (Exception err) {
                        LogUtil.Log("Open backup folder failed!", LogLevel.Warn);
                        err.LogDetailed();

                        TextMenu.Item openBackupFolder = menuItems[DialogId.Options.OpenBackupFolder];
                        TextMenu.Item openFailed = menuItems[DialogId.Subtext.OpenBackupFolderFailed];
                        if (!textMenu.Items.Contains(openFailed)) {
                            textMenu.Insert(textMenu.IndexOf(openBackupFolder) + 1, openFailed);
                        }
                    }
                });
            textMenu.Add(item);
            menuItems.Add(DialogId.Options.OpenBackupFolder, item);

            // split the string into multiple lines to prevent off-screen menus caused by long path
            string[] descriptionLines = string.Format(DialogId.Subtext.BackupLocation.DialogGet(), Path.GetFullPath(Modules.InfiniteBackups.BackupPath)).SplitIntoFixedLength(50);
            TextMenuExt.EaseInSubHeaderExt backupLocationSubtext = new TextMenuExt.EaseInSubHeaderExt(string.Join("\n", descriptionLines), false, textMenu) {
                TextColor = Color.Gray,
                HeightExtra = 0,
            };
            textMenu.Add(backupLocationSubtext);

            TextMenuExt.EaseInSubHeaderExt openFailedSubtext = new TextMenuExt.EaseInSubHeaderExt(DialogId.Subtext.OpenBackupFolderFailed.DialogClean(), false, textMenu) {
                TextColor = Color.OrangeRed,
                HeightExtra = 0f
            };

            item.OnEnter += delegate {
                openFailedSubtext.FadeVisible = true;
                backupLocationSubtext.FadeVisible = true;
            };
            item.OnLeave += delegate {
                openFailedSubtext.FadeVisible = false;
                backupLocationSubtext.FadeVisible = false;
            };

            menuItems.Add(DialogId.Subtext.BackupLocation, backupLocationSubtext);
            menuItems.Add(DialogId.Subtext.OpenBackupFolderFailed, openFailedSubtext);
        }

        public void CreateMenu(TextMenu menu, bool inGame) {
            menuItems.Clear();

            CreateEnabledEntry(menu, inGame);
#if DEBUG
            CreateLogToIngameConsoleEntry(menu, inGame);
#endif
            CreateBackupAsZipFileEntry(menu, inGame);
            CreateAutoDeleteOldBackupsEntry(menu, inGame);
            CreateDeleteBackupsOlderThanDaysEntry(menu, inGame);
            CreateDeleteBackupsAfterAmountEntry(menu, inGame);
            CreateOpenBackupFolderEntry(menu, inGame);

            RefreshItemsStates();
        }

        private void RefreshItemsStates() {
            TextMenu.Item[] visibilitySkipItems = {
                menuItems[DialogId.Options.Enabled],
                menuItems[DialogId.Options.OpenBackupFolder]
            };
            foreach (TextMenu.Item menuItem in menuItems.Values) {
                if (visibilitySkipItems.Contains(menuItem)) {
                    continue;
                }
                menuItem.Visible = Enabled;
            }

            menuItems[DialogId.Options.DeleteBackupsOlderThanDays].Disabled = !AutoDeleteOldBackups;
            menuItems[DialogId.Options.DeleteBackupsAfterAmount].Disabled = !AutoDeleteOldBackups;
        }
    }
}
