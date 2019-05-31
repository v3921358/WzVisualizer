﻿using MapleLib.WzLib;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using WzVisualizer.GUI.Controls;
using WzVisualizer.Properties;

namespace WzVisualizer {

    internal delegate void AddGridRowCallBack(DataGridView grid, BinData binData);

    public partial class MainForm : Form {
        private readonly PropertiesViewer viewer = new PropertiesViewer();
        private readonly FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
        private WzStringUtility StringUtility;
        private WzFile stringWz;
        private WzFile itemWz;
        private WzFile characterWz;
        private WzFile mapWz;
        private WzFile mobWz;
        private WzFile skillWz;
        private WzFile npcWz;
        private WzFile reactorWz;

        public MainForm() {
            InitializeComponent();

            string[] names = Enum.GetNames(typeof(WzMapleVersion));
            ComboEncType.Items.Add("AUTO-DETECT");
            for (int i = 0; i < names.Length; i++) {
                if (i == (int)WzMapleVersion.CLASSIC) break;
                ComboEncType.Items.Add(names[i]);
            }

            // Obtain the last used WZ root directory
            TextWzPath.Text = Settings.Default.PathCache;
            // Set default values for the ComboBoxes
            ComboLoadType.SelectedIndex = 0;
            ComboEncType.SelectedIndex = 0;

            foreach (var child in TabControlMain.Controls) {
                if (child is TabPage page) {
                    AddEventHandlers(page);
                }
            }
        }

        private void AddEventHandlers(TabPage page) {
            foreach (var c in page.Controls) {
                switch (c) {
                    case DataViewer view: {
                        Debug.WriteLine($"Added event handler for view: {view.Name}");
                        view.GridView.CellDoubleClick += Grid_CellDoubleClick;
                        view.GridView.CellStateChanged += Grid_RowStateChanged;
                        break;
                    }
                    case TabControl ctrl: {
                        foreach (TabPage childPage in ctrl.TabPages) {
                            AddEventHandlers(childPage);
                        }
                        break;
                    }
                }
            }
        }

        #region row append
        private void AddFaceRow(WzImage image) {
            string imgName = Path.GetFileNameWithoutExtension(image.Name);
            int id = int.Parse(imgName);
            WzObject wzObject = image.GetFromPath("blink/0/face");
            WzCanvasProperty icon;
            if (wzObject is WzUOLProperty ufo) icon = (WzCanvasProperty)ufo.LinkValue;
            else icon = (WzCanvasProperty)wzObject;
            string name = StringUtility.GetEqp(id);
            Bitmap bitmap = null;
            try { bitmap = icon?.GetBitmap(); } catch { }
            EquipFacesView.GridView.Rows.Add(id, bitmap, name, "");
        }

        private void AddHairRow(WzImage image) {
            string imgName = Path.GetFileNameWithoutExtension(image.Name);
            int id = int.Parse(imgName);
            WzCanvasProperty icon = (WzCanvasProperty)image.GetFromPath("default/hairOverHead");
            if (icon == null) {
                icon = (WzCanvasProperty)image.GetFromPath("default/hair");
            }
            string name = StringUtility.GetEqp(id);
            EquipHairsView.GridView.Rows.Add(id, icon?.GetBitmap(), name, "");
        }

        private void AddGridRow(DataGridView grid, object wzObject) {
            int id;
            string properties = "";
            string name = null;
            WzCanvasProperty icon = null;

            if (wzObject is WzImage image) {
                image.ParseImage();
                string imgName = Path.GetFileNameWithoutExtension(image.Name);
                properties = BuildProperties(image) ?? "";
                id = int.Parse(imgName);
                WzImageProperty entityIcon = image.GetFromPath("stand/0");
                WzImageProperty linkProperty = image.GetFromPath("info/link");
                if (linkProperty != null) {
                    string linkName = ((WzStringProperty)linkProperty).Value;
                    image = ((WzDirectory)image.Parent).GetChildImages().Find(p => p.Name.Equals(linkName + ".img"));
                    if (image == null) return;
                }

                if (image.WzFileParent.Name.StartsWith("Npc")) { // icon path like: '{ID}/stand/0'
                    // and also sometimes contains a link STRING property instead of using UOL
                    name = StringUtility.GetNPC(id);
                } else if (image.WzFileParent.Name.StartsWith("Mob")) {
                    // icon path like: '{ID}/(move|stand|fly)/0'
                    name = StringUtility.GetMob(id);
                    // attempt to get image of the monster
                    entityIcon = image.GetFromPath("fly/0") ?? image.GetFromPath("move/0");
                } else if (image.WzFileParent.Name.StartsWith("Reactor")) {
                    name = image.GetFromPath("action")?.WzValue.ToString();
                    entityIcon = image.GetFromPath("0/0");
                } else {  // for breadcrumb like: '{ID}.img/info/icon'
                    if (ItemConstants.IsEquip(id)) name = StringUtility.GetEqp(id);
                    else if (ItemConstants.IsPet(id)) name = StringUtility.GetPet(id);
                    icon = (WzCanvasProperty)image.GetFromPath("info/icon");
                }

                if (icon == null) {
                    if (entityIcon is WzUOLProperty uol) icon = (WzCanvasProperty)uol.LinkValue;
                    else icon = (WzCanvasProperty)entityIcon;
                }
            } else if (wzObject is WzSubProperty subProperty) {
                if (subProperty.WzFileParent.Name.StartsWith("Skill")) {
                    id = int.Parse(subProperty.Name);
                    name = StringUtility.GetSkill(subProperty.Name);

                    icon = (WzCanvasProperty)subProperty.GetFromPath("icon");
                } else { // for breadcrumb like: 'category.img/{ID}/info/icon' (etc.wz)
                    string imgName = subProperty.Name;
                    properties = BuildProperties(subProperty);
                    id = int.Parse(imgName);
                    if (ItemConstants.IsEtc(id)) name = StringUtility.GetEtc(id);
                    else if (ItemConstants.IsCash(id)) name = StringUtility.GetCash(id);
                    else if (ItemConstants.IsChair(id)) name = StringUtility.GetChair(id);
                    else if (ItemConstants.IsConsume(id)) name = StringUtility.GetConsume(id);

                    WzImageProperty imgIcon = subProperty.GetFromPath("info/icon");
                    if (imgIcon is WzUOLProperty ufo) imgIcon = (WzCanvasProperty)ufo.LinkValue;
                    else if (imgIcon is WzCanvasProperty canvas) imgIcon = canvas;
                    if (imgIcon != null) icon = (WzCanvasProperty)imgIcon;
                }
            } else
                return;
            Bitmap bitmap = null;
            try { bitmap = icon?.GetBitmap(); } catch (Exception) { }
            grid.Rows.Add(id, bitmap, name, properties);
        }
        #endregion

        /// <summary>
        /// Concatenate all properties excluding image and sound properties
        /// </summary>
        /// <param name="wzObject">a WzSubProperty or WzImage</param>
        /// <returns></returns>
        private string BuildProperties(object wzObject) {
            string properties = "";
            WzImageProperty infoRoot = null;
            if (wzObject is WzSubProperty subProperty) infoRoot = subProperty.GetFromPath("info");
            else if (wzObject is WzImage wzImage) infoRoot = wzImage.GetFromPath("info");
            if (infoRoot?.WzProperties != null) {
                foreach (WzImageProperty imgProperties in infoRoot.WzProperties) {
                    switch (imgProperties.PropertyType) {
                        default:
                            properties += $"\r\n{imgProperties.Name}={imgProperties.WzValue}";
                            break;
                        case WzPropertyType.Canvas:
                        case WzPropertyType.PNG:
                        case WzPropertyType.Sound:
                            break;
                    }
                }

            }
            return properties;
        }

        private void LoadWzData(WzMapleVersion mapleVersion, string mapleDirectory) {
            int selectedRoot = TabControlMain.SelectedIndex;
            switch (TabControlMain.SelectedTab.Controls[0]) {
                case DataViewer view: {
                    view.GridView.Rows.Clear();
                    break;
                }
                case TabControl ctrl: {
                    if (ctrl.SelectedTab.Controls[0] is DataViewer view) view.GridView.Rows.Clear();
                    break;
                }
            }
            ((DataViewer)EquipTab.SelectedTab.Controls[0]).GridView.Rows.Clear();
            switch (selectedRoot) {
                default:
                    Debug.WriteLine($"Unable to load WZ data unhandled selected index: {TabControlMain.SelectedIndex}");
                    break;
                case 0: // Equips
                    {
                    if (!LoadWzFileIfAbsent(ref characterWz, mapleDirectory + "/Character", mapleVersion)) return;
                    List<WzImage> children = characterWz.WzDirectory.GetChildImages();
                    children.Sort((a, b) => a.Name.CompareTo(b.Name));
                    for (int i = 0; i < characterWz.WzDirectory.CountImages(); i++) {
                        WzImage image = children[i];
                        string name = Path.GetFileNameWithoutExtension(image.Name);
                        if (int.TryParse(name, out int equipId)) {
                            int selectedTab = EquipTab.SelectedIndex;
                            int bodyPart = equipId / 10000;
                            switch (bodyPart) {
                                default:
                                    if (selectedTab == 2 && bodyPart >= 130 && bodyPart <= 170) AddGridRow(EquipWeaponsView.GridView, image);
                                    else if (selectedTab == 1 && bodyPart == 2) AddFaceRow(image);
                                    else if (selectedTab == 0 && bodyPart == 3) AddHairRow(image);
                                    break;
                                case 100: // Caps
                                    if (selectedTab == 4) AddGridRow(EquipCapsView.GridView, image);
                                    break;
                                case 101:
                                case 102:
                                case 103:
                                case 112:
                                case 113:
                                case 114: // Accessory
                                    if (selectedTab == 3) AddGridRow(EquipAccessoryView.GridView, image);
                                    break;
                                case 110: // Cape
                                    if (selectedTab == 9) AddGridRow(EquipCapesView.GridView, image);
                                    break;
                                case 104: // Coat
                                    if (selectedTab == 6) AddGridRow(EquipTopsView.GridView, image);
                                    break;
                                case 108: // Glove
                                    if (selectedTab == 10) AddGridRow(EquipGlovesView.GridView, image);
                                    break;
                                case 105: // Longcoat
                                    if (selectedTab == 5) AddGridRow(EquipsOverallsView.GridView, image);
                                    break;
                                case 106: // Pants
                                    if (selectedTab == 7) AddGridRow(EquipPantsView.GridView, image);
                                    break;
                                case 180:
                                case 181:
                                case 182:
                                case 183: // Pet Equips
                                          // image.ParseImage();
                                    break;
                                case 111: // Rings
                                    if (selectedTab == 11) AddGridRow(EquipRingsView.GridView, image);
                                    break;
                                case 109: // Shield
                                    if (selectedTab == 12) AddGridRow(EquipShieldsView.GridView, image);
                                    break;
                                case 107: // Shoes
                                    if (selectedTab == 8) AddGridRow(EquipShoesView.GridView, image);
                                    break;
                                case 190:
                                case 191:
                                case 193: // Taming Mob
                                    //if (selectedTab == 13) AddGridRow(, image);
                                    break;
                            }
                        }
                    }
                    break;
                }
                case 1: // Use
                case 2: // Setup
                case 3: // Etc
                case 4: // Cash
                case 9: // Pets
                    {
                    if (!LoadWzFileIfAbsent(ref itemWz, mapleDirectory + "/Item", mapleVersion)) return;
                    List<WzImage> children = itemWz.WzDirectory.GetChildImages();
                    children.Sort((a, b) => a.Name.CompareTo(b.Name));
                    for (int i = 0; i < itemWz.WzDirectory.CountImages(); i++) {
                        WzImage image = children[i];
                        string name = Path.GetFileNameWithoutExtension(image.Name);
                        if (int.TryParse(name, out int itemId)) {
                            switch (itemId) {
                                default:
                                    image.ParseImage();
                                    if (selectedRoot == 9 && ItemConstants.IsPet(itemId)) // pet
                                        AddGridRow(PetsView.GridView, image);
                                    if (selectedRoot == 3 && ItemConstants.IsEtc(itemId)) // etc
                                        image.WzProperties.ForEach(img => AddGridRow(EtcView.GridView, img));
                                    if (selectedRoot == 4 && ItemConstants.IsCash(itemId)) // cash
                                        image.WzProperties.ForEach(img => AddGridRow(CashView.GridView, img));
                                    if (selectedRoot == 1 && ItemConstants.IsConsume(itemId)) // consume
                                        image.WzProperties.ForEach(img => AddGridRow(UseConsumeView.GridView, img));
                                    break;
                                case 204: // scrolls
                                    if (selectedRoot == 1)
                                        image.WzProperties.ForEach(img => AddGridRow(UseScrollsView.GridView, img));
                                    break;
                                case 206:
                                case 207:
                                case 233: // projectiles
                                    if (selectedRoot == 1)
                                        image.WzProperties.ForEach(img => AddGridRow(UseProjectileView.GridView, img));
                                    break;
                                case 301: // chairs
                                case 399: // x-mas characters
                                    if (selectedRoot == 2)
                                        image.WzProperties.ForEach(img => AddGridRow((itemId == 301 ? SetupChairsView : SetupOthersView).GridView, img));
                                    break;
                            }
                        }
                    }
                    break;
                }
                case 5: // Map
                    {
                    if (!LoadWzFileIfAbsent(ref mapWz, mapleDirectory + "/Map", mapleVersion)) return;
                    List<WzImage> children = mapWz.WzDirectory.GetChildImages();
                    children.Sort((a, b) => a.Name.CompareTo(b.Name));
                    for (int i = 0; i < mapWz.WzDirectory.CountImages(); i++) {
                        WzImage image = children[i];
                        string sMapId = Path.GetFileNameWithoutExtension(image.Name);
                        if (int.TryParse(sMapId, out int mapId)) {
                            image.ParseImage();
                            string properties = BuildProperties(image);
                            WzCanvasProperty icon = (WzCanvasProperty)image.GetFromPath("miniMap/canvas");
                            string name = StringUtility.GetFieldFullName(mapId);

                            MapsView.GridView.Rows.Add(mapId, icon?.GetBitmap(), name, properties);
                        }
                    }
                    break;
                }
                case 6: // Mob
                    {
                    if (!LoadWzFileIfAbsent(ref mobWz, mapleDirectory + "/Mob", mapleVersion)) return;
                    MobsView.GridView.Rows.Clear();

                    List<WzImage> children = mobWz.WzDirectory.GetChildImages();
                    children.Sort((a, b) => a.Name.CompareTo(b.Name));
                    for (int i = 0; i < mobWz.WzDirectory.CountImages(); i++) {
                        WzImage image = children[i];
                        AddGridRow(MobsView.GridView, image);
                    }
                    break;
                }
                case 7: // Skills
                    {
                    if (!LoadWzFileIfAbsent(ref skillWz, mapleDirectory + "/Skill", mapleVersion)) return;
                    SkillsView.GridView.Rows.Clear();

                    List<WzImage> children = skillWz.WzDirectory.GetChildImages();
                    children.Sort((a, b) => a.Name.CompareTo(b.Name));
                    for (int i = 0; i < skillWz.WzDirectory.CountImages(); i++) {
                        WzImage image = children[i];
                        string name = Path.GetFileNameWithoutExtension(image.Name);
                        if (int.TryParse(name, out _)) {
                            WzImageProperty tree = image.GetFromPath("skill");
                            if (tree is WzSubProperty) {
                                List<WzImageProperty> skills = tree.WzProperties;
                                skills.ForEach(s => AddGridRow(SkillsView.GridView, s));
                                skills.Clear();
                            }
                        }
                    }
                    break;
                }
                case 8: // NPCs
                {
                    if (!LoadWzFileIfAbsent(ref npcWz, mapleDirectory + "/Npc", mapleVersion)) return;
                    NPCView.GridView.Rows.Clear();

                    List<WzImage> children = npcWz.WzDirectory.GetChildImages();
                    children.Sort((a, b) => a.Name.CompareTo(b.Name));
                    for (int i = 0; i < npcWz.WzDirectory.CountImages(); i++) {
                        WzImage image = children[i];
                        AddGridRow(NPCView.GridView, image);
                    }
                    break;
                }
                case 10: // Reactors
                {
                    if (!LoadWzFileIfAbsent(ref reactorWz, mapleDirectory + "/Reactor", mapleVersion)) return;
                    ReactorView.GridView.Rows.Clear();

                    List<WzImage> children = reactorWz.WzDirectory.GetChildImages();
                    children.Sort((a, b) => a.Name.CompareTo(b.Name));
                    for (int i = 0; i < reactorWz.WzDirectory.CountImages(); i++) {
                        WzImage image = children[i];
                        AddGridRow(ReactorView.GridView, image);
                    }
                    break;
                }
            }
        }

        private bool LoadWzFileIfAbsent(ref WzFile wzFile, string fileName, WzMapleVersion mapleVersion) {
            if (wzFile != null) return false;
            if (File.Exists(fileName + Resources.FileExtension)) {
                wzFile = new WzFile(fileName + Resources.FileExtension, mapleVersion);
                wzFile.ParseWzFile();
                return true;
            } else { // KMS
                wzFile = new WzFile(fileName, mapleVersion);
                WzDirectory dir = new WzDirectory(fileName, wzFile);
                wzFile.WzDirectory = dir;
                RecursivelyLoadDirectory(dir, fileName, mapleVersion);
                return true;
            }
        }

        private void RecursivelyLoadDirectory(WzDirectory dir, string directoryPath, WzMapleVersion mapleVersion) {
            if (!Directory.Exists(directoryPath)) return;
            string[] files = Directory.GetFiles(directoryPath);
            foreach (string file in files) {
                FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read);
                WzImage img = new WzImage(Path.GetFileName(file), stream, mapleVersion);
                dir.AddImage(img);
            }
            files = Directory.GetDirectories(directoryPath);
            foreach (string sub in files) {
                WzDirectory subDir = new WzDirectory(Path.GetFileNameWithoutExtension(sub));
                RecursivelyLoadDirectory(subDir, sub, mapleVersion);
                dir.AddDirectory(subDir);
            }
        }

        /// <summary>
        /// clears all collections, closes underlying file readers 
        /// then calls the garbage collector for each loaded WZ file
        /// </summary>
        private void DisposeWzFiles() {
            stringWz?.Dispose();
            itemWz?.Dispose();
            characterWz?.Dispose();
            mapWz?.Dispose();
            mobWz?.Dispose();
            skillWz?.Dispose();
            npcWz?.Dispose();

            stringWz = null;
            itemWz = null;
            characterWz = null;
            mapWz = null;
            mobWz = null;
            skillWz = null;
            npcWz = null;
        }

        /// <summary>
        /// Add a row data to the specified grid view using 
        /// parsed bin data
        /// </summary>
        /// <param name="grid">the grid view to add a row to</param>
        /// <param name="binData">bin data (wz files that were parsed then saved as a bin file type)</param>
        public void AddGridRow(DataGridView grid, BinData binData) {
            string allProperties = "";
            foreach (string prop in binData.properties)
                allProperties += prop + "\r\n";

            string filter = SearchTextBox.Text;
            if (filter?.Length > 0 && !binData.Search(filter))
                return;

            if (!IsDisposed && InvokeRequired) {
                Image image = binData?.image;
                Invoke(new Action(() => {
                    grid.Rows.Add(binData.ID, image, binData.Name, allProperties);
                }));
            }
        }

        #region event handling

        /// <summary>
        /// Update the Window's clipboard when a cell is selected
        /// </summary>
        private void Grid_RowStateChanged(object sender, DataGridViewCellStateChangedEventArgs e) {
            switch (e.Cell.Value) {
                case int i:
                    Clipboard.SetText(i.ToString());
                    break;
                case string str when str.Length > 0:
                    Clipboard.SetText(str);
                    break;
                default:
                    Clipboard.Clear();
                    break;
            }
        }

        /// <summary>
        /// Display the PropertiesViewer Form when a Properties column cell is double clicked
        /// </summary>
        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e) {
            if (e.ColumnIndex == 3 && e.RowIndex != -1) {
                viewer.SetProperties((string)((DataGridView)sender).SelectedCells[0].Value);
                viewer.Show();
                viewer.BringToFront();
            }
        }

        /// <summary>
        /// Open the FolderBrowser dialog window when the text box is clicked and set the selected
        /// directory as the root folder containing WZ files
        /// </summary>
        private void TextWzPath_Click(object sender, EventArgs e) {
            if (folderBrowser.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowser.SelectedPath)) {
                TextWzPath.Text = folderBrowser.SelectedPath;
            }
        }

        /// <summary>
        /// Begin loading WZ data corresponding to the selected tab
        /// </summary>
        private void BtnWzLoad_Click(object sender, EventArgs e) {
            string folderPath = TextWzPath.Text;
            if (folderPath.Length > 0) {
                if (!folderPath.Equals(Settings.Default.PathCache)) {
                    Settings.Default.PathCache = folderPath;
                    Settings.Default.Save();
                }
                string stringWzPath = folderPath + @"/String";
                WzMapleVersion mapleVersion;

                if (File.Exists(stringWzPath + Resources.FileExtension)) {
                    if (ComboEncType.SelectedIndex == 0)
                        mapleVersion = WzTool.DetectMapleVersion(stringWzPath + Resources.FileExtension, out _);
                    else mapleVersion = (WzMapleVersion)
                        ComboEncType.SelectedIndex - 1;

                    stringWz = new WzFile(stringWzPath + Resources.FileExtension, mapleVersion);
                    stringWz.ParseWzFile();
                    short? version = stringWz.Version;
                    if (WzTool.GetDecryptionSuccessRate(stringWzPath + Resources.FileExtension, mapleVersion, ref version) < 0.8) {
                        MessageBox.Show(Resources.BadEncryption, Resources.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                } else if (Directory.Exists(stringWzPath)) { // KMS
                    mapleVersion = WzMapleVersion.EMS;
                    stringWz = new WzFile(stringWzPath, mapleVersion);
                    WzDirectory dir = new WzDirectory("String", stringWz);
                    stringWz.WzDirectory = dir;
                    RecursivelyLoadDirectory(dir, stringWzPath, mapleVersion);
                } else {
                    MessageBox.Show(Resources.MissingStringFile, Resources.FIleNotFound, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DisposeWzFiles();
                    return;
                }
                StringUtility = new WzStringUtility(stringWz);
                LoadWzData(mapleVersion, folderPath);
            }
            DisposeWzFiles();
        }

        /// <summary>
        /// Enable or disable the WZ path controls if BIN loading is selected
        /// </summary>
        private void ComboLoadType_SelectedIndexChanged(object sender, EventArgs e) {
            bool enabled = (ComboLoadType.SelectedIndex == 1);
            TextWzPath.Enabled = enabled;
            BtnWzLoad.Enabled = enabled;
        }

        /// <summary>
        /// upon clicking the save button, store data of the current opened grid.
        /// Some tabs may have another TabControl in which that Control contains a Grid control.
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs ev) {
            MouseEventArgs e = (MouseEventArgs)ev;
            switch (e.Button) {
                case MouseButtons.Left: {
                    switch (TabControlMain.SelectedTab.Controls[0]) {
                        case DataViewer view:   // no child tabs and contains 1 child Control (DataGridView)
                            GridIOUtility.ExportGrid(view, TabControlMain.SelectedTab.Text);
                            break;
                        case TabControl tab: // contains child controls (e.g. Equips.Hairs, Equips.Faces)
                            GridIOUtility.ExportGrid((DataViewer)tab.SelectedTab.Controls[0], TabControlMain.SelectedTab.Text); // The DataGridView contained in the TabPage control
                            break;
                    }
                    MessageBox.Show(Resources.CompleteSaveBIN);
                    break;
                }
                case MouseButtons.Right: {
                    var control = TabControlMain.SelectedTab.Controls[0];
                    if (control is DataGridView grid) // no child tabs and contains 1 child Control (DataGridView)
                        GridIOUtility.ExportGridImages(grid, TabControlMain.SelectedTab.Text);
                    else if (control is TabControl tab) { // contains child controls (e.g. Equips.Hairs, Equips.Faces)
                        control = tab.SelectedTab; // The selected child Tab (e.g. Equips.Hairs)
                        GridIOUtility.ExportGridImages((DataGridView)control.Controls[0], TabControlMain.SelectedTab.Text); // The DataGridView contained in the TabPage control
                    }
                    MessageBox.Show(Resources.CompleteSaveImages);
                    break;
                }
            }
        }

        #region tab change events

        private void TabEquips_Selected(object sender, TabControlEventArgs e) {
            Tab_Selected(sender, e.TabPage);
        }

        private void TabUse_Selected(object sender, TabControlEventArgs e) {
            Tab_Selected(sender, e.TabPage);
        }

        private void TabSetup_Selected(object sender, TabControlEventArgs e) {
            Tab_Selected(sender, e.TabPage);
        }

        private void TabControlMain_SelectedIndexChanged(object sender, EventArgs e) {
            Tab_Selected(sender, GetSelectedTab());
        }

        private void Tab_Selected(object sender, TabPage tab) {
            FieldInfo[] fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields) {
                var obj = field.GetValue(this);

                // release memory for the current tab
                if (obj is DataViewer view) {
                    view.GridView.Rows.Clear();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            switch (TabControlMain.SelectedTab.Controls[0]) {
                case DataViewer view: {
                    // no child control
                    GridIOUtility.ImportGrid($"{TabControlMain.SelectedTab.Text}/{tab.Text}.bin", view.GridView,
                        AddGridRow);
                    break;
                }
                case TabControl childTab: {
                    DataViewer view = (DataViewer) childTab.SelectedTab.Controls[0];
                    GridIOUtility.ImportGrid($"{TabControlMain.SelectedTab.Text}/{tab.Text}.bin", view.GridView,
                        AddGridRow);
                    break;
                }
            }
        }

        #endregion

        private void MainForm_Load(object sender, EventArgs e) {
            GridIOUtility.ImportGrid("equips/Hairs.bin", EquipHairsView.GridView, AddGridRow);
        }

        private void SearchTextBox_KeyPress(object sender, KeyPressEventArgs e) {
            int keyCode = (int)e.KeyChar;
            if (keyCode == (int)Keys.Enter)
                Tab_Selected(sender, GetSelectedTab());
        }
        #endregion

        private TabPage GetSelectedTab() {
            TabPage root = TabControlMain.SelectedTab;
            object control = root.Controls[0];
            return (control is TabControl tab) ? tab.SelectedTab : root;
        }
    }
}
