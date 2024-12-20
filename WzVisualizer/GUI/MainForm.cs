﻿
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using WzVisualizer.GUI.Controls;
using WzVisualizer.IO;
using WzVisualizer.Properties;
using WzVisualizer.Util;

namespace WzVisualizer.GUI {
    internal delegate void AddGridRowCallBack(DataGridView grid, BinData binData);

    public partial class MainForm : Form {
        internal readonly SearchForm SearchForm = new();

        private readonly FolderSelectDialog folderBrowser = new();
        private readonly PropertiesViewer viewer = new();
        public bool LoadAll { get; set; }

        public MainForm() {
            InitializeComponent();

            // set the default path to the current directory
            wzPathTextbox.Text = Directory.GetCurrentDirectory();

            AddOwnedForm(SearchForm);
            SearchForm.Location = new Point(Right - SearchForm.Width - 5, Top + SearchForm.Height / 2);
            LocationChanged += (o, args) => SearchForm.Location = new Point(Right - SearchForm.Width - 5, Top + SearchForm.Height / 2);
            SearchForm.searchButton.Click += (o, args) => OnTabControlChanged();
        }

        public string SearchQuery => searchTextbox.Text;

        /// <summary>
        /// recursively add event handlers to all DataViewport components
        /// </summary>
        private void AddEventHandlers(TabControl tab) {
            foreach (Control ctrl in tab.Controls) {
                switch (ctrl.Controls[0]) {
                    case DataViewport dv:
                        dv.Data = new List<BinData>(); // cheeky little initializer

                        dv.GridView.CellDoubleClick += Grid_CellDoubleClick;
                        dv.GridView.CellStateChanged += Grid_RowStateChanged;
                        break;
                    case TabControl subTab:
                        subTab.Selected += TabControl_Selected;
                        AddEventHandlers(subTab);
                        break;
                }
            }
        }

        public DataViewport GetCurrentDataViewport() {
            var main = TabControlMain.SelectedTab;
            var sub = (main.Controls[0] is TabControl tc ? tc.SelectedTab : main);
            return (DataViewport)sub.Controls[0];
        }

        /// <summary>
        /// clears all collections, closes underlying file readers 
        /// then calls the garbage collector for each loaded WZ file
        /// </summary>
        private static void DisposeWzFiles() {
            foreach (var wz in Enum.GetValues(typeof(Wz)).Cast<Wz>()) {
                wz.Dispose();
            }
        }

        private void LoadWzData() {
            if (LoadAll) {
                for (var i = 0; i < TabControlMain.TabCount; i++) {
                    TabControlMain.SelectedIndex = i;
                    VisualizerUtil.ProcessTab(i, this);
                }
                BtnSave_Click(null, new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0));
                return;
            }

            VisualizerUtil.ProcessTab(TabControlMain.SelectedIndex, this);
        }

        /// <summary>
        /// Begin loading WZ data corresponding to the selected tab
        /// </summary>
        private void BtnWzLoad_Click(object sender, EventArgs e) {
            ClearAllPages(TabControlMain, true);
            DisposeWzFiles();

            LoadAll = ModifierKeys == Keys.Shift;

            if (LoadAll) {
                var result = MessageBox.Show(Resources.MassReadWarning, Resources.Warning, MessageBoxButtons.YesNo);
                if (result != DialogResult.Yes) return;
            }

            var path = wzPathTextbox.Text;

            if (!path.Equals(Settings.Default.PathCache)) {
                Settings.Default.PathCache = path;
                Settings.Default.Save();
            }

            if (string.IsNullOrEmpty(path)) {
                goto NO_FILES;
            }

            // 64-bit client update
            if (Directory.Exists(($@"{path}\Data"))) {
                foreach (var wz in Enum.GetValues(typeof(Wz)).Cast<Wz>()) {
                    try {
                        var files = Directory.GetFiles($@"{path}\Data\{wz}", "*.wz", SearchOption.AllDirectories);
                        foreach (var file in files) {
                            var name = Path.GetFileNameWithoutExtension(file);

                            // MapleLib fails to parse
                            if (name.StartsWith("script", StringComparison.CurrentCultureIgnoreCase)) continue;

                            wz.Load(file);
                        }
                    } catch (DirectoryNotFoundException) {
                        // DirectoryNotFoundException : List.wz removed
                    }
                }
                LoadWzData();
                return;
            }

            var testFile = path + $@"\String{Resources.FileExtensionWZ}";
            // classic
            if (File.Exists(testFile)) {
                foreach (var wz in Enum.GetValues(typeof(Wz)).Cast<Wz>()) {

                    // not necessary, and cannot parse in this way
                    if (wz == Wz.List) continue;

                    wz.Load(@$"{path}\{wz}{Resources.FileExtensionWZ}", false);
                }
                LoadWzData();
                return;
            }

            // BMS
            if (Directory.Exists(testFile) && File.Exists($@"{testFile}\Cash.img")) {
                foreach (var wz in Enum.GetValues(typeof(Wz)).Cast<Wz>()) {
                    wz.Load(@$"{path}\{wz}{Resources.FileExtensionWZ}");
                }
                LoadWzData();
                return;
            }

        NO_FILES:
            MessageBox.Show(Resources.GameFilesNotFound, Resources.FileNotFound, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        void ExportBinary(TabControl tab, bool saveAll = false) {
            var selectedTab = tab.SelectedTab;
            if (saveAll) {
                for (var i = 0; i < TabControlMain.TabCount; i++) {
                    TabControlMain.SelectedIndex = i;
                    BinaryDataUtil.ExportBinary(TabControlMain.TabPages[i], TabControlMain.TabPages[i].Text);
                }
            } else BinaryDataUtil.ExportBinary(selectedTab, selectedTab.Text);
        }

        void ExportPictures(TabControl tab, bool saveAll = false) {
            var selectedTab = tab.SelectedTab;
            if (saveAll) {
                for (var i = 0; i < TabControlMain.TabCount; i++) {
                    TabControlMain.SelectedIndex = i;
                    BinaryDataUtil.ExportPictures(TabControlMain.TabPages[i], TabControlMain.TabPages[i].Text);
                }
            } else BinaryDataUtil.ExportPictures(selectedTab, selectedTab.Text);
        }

        /// <summary>
        /// upon clicking the save button, store data of the current opened grid.
        /// Some tabs may have another TabControl in which that Control contains a Grid control.
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs ev) {
            var button = ((MouseEventArgs)ev).Button;
            var saveAll = LoadAll || ModifierKeys == Keys.Shift;

            if (!LoadAll && saveAll) {
                var result = MessageBox.Show(Resources.MassWriteWarning, Resources.Warning, MessageBoxButtons.YesNo);
                if (result != DialogResult.Yes) return;
            }

            ExportBinary(TabControlMain, saveAll);
            MessageBox.Show(Resources.CompleteSaveBIN, Resources.SaveComplete);
            LoadAll = false;
        }

        private void BtnExport_Click(object sender, EventArgs e) {
            ExportPictures(TabControlMain, true);
            MessageBox.Show(Resources.CompleteSaveImages, Resources.SaveComplete);
        }

        /// <summary>
        /// Update the Window's clipboard when a cell is selected
        /// </summary>
        private static void Grid_RowStateChanged(object sender, DataGridViewCellStateChangedEventArgs e) {
            try {
                switch (e.Cell.Value) {
                    case int i:
                        Clipboard.SetText(i.ToString());
                        break;
                    case string { Length: > 0 } str:
                        Clipboard.SetText(str);
                        break;
                    case Bitmap { Width: >= 0, Height: >= 0 } image:
                        Clipboard.SetImage(image);
                        break;
                    default:
                        Clipboard.Clear();
                        break;
                }
            } catch (Exception) {
                // ExternalException        "typically occurs when the Clipboard is being used by another process."
                // ArgumentNullException    "text is null or Empty."
            }
        }

        /// <summary>
        /// Display the PropertiesViewer Form when a Properties column cell is double clicked
        /// </summary>
        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex < 0) return;

            var grid = (DataGridView)sender;
            var cell = grid.SelectedCells[0];

            switch (cell.ColumnIndex) {
                case 0:
                    Clipboard.SetText($"!item {cell.Value}");
                    break;
                case 1:
                    var bmp = cell.Value as Bitmap;
                    if (bmp == null) return;
                    cell.OwningColumn.Width = bmp.Width + 15;
                    cell.OwningRow.Height = bmp.Height + 15;
                    break;
                case 3:
                    viewer.SetProperties((string)((DataGridView)sender).SelectedCells[0].Value);
                    if (!viewer.Visible) {
                        viewer.Height = Height;
                        viewer.StartPosition = FormStartPosition.Manual;

                        viewer.Left = Right;
                        viewer.Top = Top;
                    }
                    viewer.Show();
                    viewer.BringToFront();
                    break;
            }
        }

        /// <summary>
        /// Open the FolderBrowser dialog window when the text box is clicked and set the selected
        /// directory as the root folder containing WZ files
        /// </summary>
        private void TextWzPath_Click(object sender, EventArgs e) {
            if (!folderBrowser.ShowDialog(Handle)) return;
            wzPathTextbox.Text = folderBrowser.FileName;
        }


        private void SearchTextBox_KeyPress(object sender, KeyPressEventArgs e) {
            if (e.KeyChar == 13) {
                OnTabControlChanged();
            }
        }

        internal void MainForm_KeyDown(object sender, KeyEventArgs e) {
            LoadAll = ModifierKeys == Keys.Shift;
            saveButton.Text = LoadAll ? Resources.SaveAll : Resources.Save;
            loadButton.Text = LoadAll ? Resources.LoadAll : Resources.Load;
            searchButton.Text = LoadAll ? Resources.Options : Resources.Search;
        }

        internal void MainForm_KeyUp(object sender, KeyEventArgs e) {
            LoadAll = ModifierKeys == Keys.Shift;
            saveButton.Text = LoadAll ? Resources.SaveAll : Resources.Save;
            loadButton.Text = LoadAll ? Resources.LoadAll : Resources.Load;
            searchButton.Text = LoadAll ? Resources.Options : Resources.Search;
        }

        private void MainForm_Load(object sender, EventArgs e) {
            // Obtain the last used WZ root directory
            wzPathTextbox.Text = Settings.Default.PathCache;

            TabControlMain.Selected += TabControl_Selected;
            AddEventHandlers(TabControlMain);

            OnTabControlChanged();
        }

        private void TabControl_Selected(object sender, TabControlEventArgs e) {
            OnTabControlChanged();
        }

        private void BtnSearch_Click(object sender, EventArgs e) {
            if (ModifierKeys == Keys.Shift) {
                SearchForm.Show();
            } else {
                // re-load the tab, but this time we should have a search query
                OnTabControlChanged();
            }
        }

        private void OnTabControlChanged() {
            ClearAllPages(TabControlMain);

            var main = TabControlMain.SelectedTab;
            var dv = GetCurrentDataViewport();
            BinaryDataUtil.ImportGrid($"{main.Text}/{dv.Parent.Text}.bin", dv, (grid, data) => VisualizerUtil.AddNewRow(this, grid, data));
        }

        /// <summary>
        /// Clear all DataViewport grids to allow re-populating data, especially when search queries are present
        /// </summary>
        private void ClearAllPages(TabControl tabControl, bool clearData = false) {
            foreach (TabPage page in tabControl.TabPages) {
                switch (page.Controls[0]) {
                    case DataViewport dv: {
                            if (clearData) dv.Data.Clear();
                            dv.GridView.Rows.Clear();
                            break;
                        }
                    case TabControl tc:
                        if (tc == TabControlMain && tc.SelectedTab == TabControlMain.SelectedTab)
                            break;
                        ClearAllPages(tc);
                        break;
                }
            }
            GC.Collect();
        }
    }
}