﻿using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using CETextBoxControl;
using System.Diagnostics;
using Microsoft.VisualBasic.ApplicationServices;
using System.Reflection;

namespace TextEditor
{
    public partial class TextEditor : Form
    {
        /// <summary>
        /// 現在選択中のエンコード
        /// </summary>
        private Encoding m_encode = Encoding.GetEncoding(Ude.Charsets.SHIFT_JIS);

        /// <summary>
        /// 現在選択中のエンコード
        /// </summary>
        private List<Encoding> m_selectEncode = new List<Encoding>();
#if true // 検索ダイアログ未使用
        /// <summary>
        /// 検索中の文字列
        /// </summary>
        private string findString;

        /// <summary>
        /// 検索中の大文字／小文字区別区分
        /// </summary>
        private Boolean m_ul;

        private Boolean m_word;

        private Boolean m_reg;
#endif

        /// <summary>
        /// ドラッグ中フラグ
        /// true:ドラッグ中 / false:ドラッグ中でない
        /// </summary>
        private Boolean dragFlag;

        //private CEDocument m_doc;

        // ビュー管理情報
        private List<ViewInfoMng> m_viewInfoMng;

        public string openFilePath;

        /// <summary>
        /// 現在アクティブなビュー管理情報
        /// </summary>
#if false
        private ViewInfoMng m_activeViewInfo
        {
            get
            {
                for (int idx = 0; idx < m_viewInfoMng.Count; idx++)
                {
                    if (m_viewInfoMng[idx].active ==  true)
                    {
                        return m_viewInfoMng[idx];
                    }
                }

                // アクティブ情報なし
                return null;
            }
        }
#endif
        private ViewInfoMng GetActiveViewInfo()
        {
            int idx = GetActiveTabIndex();
            if (idx == -1) return null;
            return m_viewInfoMng[idx];
#if false
            foreach( ViewInfoMng vim in m_viewInfoMng)
            {
                if (vim.active) return vim;
            }

            // アクティブ情報なし
            return null;
#endif
        }

        /// <summary>
        /// 現在アクティブなビューのインデックス
        /// </summary>
#if false
        private int m_activeViewIndex
        {
            get
            {
                int idx;
                for (idx = 0; idx < m_viewInfoMng.Count; idx++)
                {
                    if (m_viewInfoMng[idx].active == true)
                    {
                        break;
                    }
                }

                // アクティブ情報なし
                return idx;
            }
        }
#endif
        private int GetActiveViewIndex()
        {
            for (int idx = 0; idx < m_viewInfoMng.Count; idx++)
            {
                if (m_viewInfoMng[idx].active == true)
                {
                    return idx;
                }
            }

            // アクティブ情報なし
            return -1;
        }

        /// <summary>
        /// キャレット位置の表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CaretPosition(object sender, CaretPositionEventArgs e)
        {
            //返されたデータを取得し表示
            StatusLabel_CaretX.Text = (e.x + 1).ToString() + ":桁";
            StatusLabel_CaretY.Text = (e.y + 1).ToString() + ":行";
            toolStripDropDownBtn.Text = e.encode;
            StatusBar.Refresh();
        }

        /// <summary>
        /// 対応するエンコードを設定
        /// </summary>
        private void SetCorrespondingEncoding()
        {
            m_selectEncode.Add(Encoding.GetEncoding(932));        // SJIS
            m_selectEncode.Add(Encoding.GetEncoding(50220));      // JIS
            m_selectEncode.Add(Encoding.GetEncoding(51932));      // EUC-JP
            m_selectEncode.Add(Encoding.GetEncoding(1200));       // UTF-16
            m_selectEncode.Add(Encoding.GetEncoding(1201));       // UTF-16BE
            m_selectEncode.Add(Encoding.GetEncoding(65001));      // UTF-8
            m_selectEncode.Add(Encoding.GetEncoding(65000));      // UTF-7
        }

        private MainApp m_MainApp;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TextEditor(MainApp ma)
        {
            // TextEditor初期化
            InitTextEditor();

            m_MainApp = ma;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="ce"></param>
        public TextEditor(CEEditView ce)
        {
            // TextEditor初期化
            InitTextEditor();

            // 新規ビュー作成
            NewEditView(ce);

            // タブ切替
            ChangeTab(0);
        }

        /// <summary>
        /// TextEditor初期化
        /// </summary>
        private void InitTextEditor()
        {
            // コンポーネント初期化
            InitializeComponent();

            // 対応するエンコードを設定
            SetCorrespondingEncoding();

            // ステータスバーにエンコードを表示する
            foreach (Encoding ei in m_selectEncode)
            {
                toolStripDropDownBtn.DropDownItems.Add(ei.EncodingName);
            }
            toolStripDropDownBtn.Text = m_encode.EncodingName;

            //ステータスバーのオーバーフロー機能を可能にする
            menuStrip1.CanOverflow = true;
            menuStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;

            tabPanel.BackColor = SystemColors.Control;//Color.Gainsboro;
            tabPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            // ビュー管理情報作成
            m_viewInfoMng = new List<ViewInfoMng>();

            // ドラッグ中フラグ
            dragFlag = false;

            // ビュー閉じるボタン追加
            AddViewCloseButton();
#if false // 検索パネル未使用
            this.KeyPress += new KeyPressEventHandler(keyPress);
#endif
        }

#region イベント

#if false // 検索パネル未使用
        /// <summary>
        /// キープレス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void keyPress(object sender, KeyPressEventArgs e)
        {
            // エスケープキーが押されたか
            if (e.KeyChar == (char)Keys.Escape)
            {
                // 検索パネルが表示されている場合は非表示に
                if (m_activeViewInfo.findPanel != null && m_activeViewInfo.findPanel != null && m_activeViewInfo.findPanel.Visible)
                {
                    m_activeViewInfo.findPanel.Visible = false;
                }
                // 検索パネルが表示されていいなければ、編集ビューのエスケープ処理を呼ぶ
                else
                {
                    m_activeViewInfo.customTextBox.EscapeKey();
                }
            }
            // 検索パネルの検索コンボボックスにフォーカスが当たっている場合、検索する
            else if (e.KeyChar == (char)Keys.Enter && m_activeViewInfo.findPanel != null && m_activeViewInfo.findPanel.findComboBox.Focused)
            {
                m_activeViewInfo.findPanel.NextFind();
            }
        }
#endif
        /// <summary>
        /// タブボタン離すイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabButton_MouseUp(object sender, MouseEventArgs e)
        {
            // マウス左クリック以外無効
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            Cursor.Current = Cursors.Default;
            Point sp = Cursor.Position;
            
            if (m_DestinationIndex != -1)
            {
                // タブビューの同一画面内での移動

                ViewInfoMng vim = GetActiveViewInfo()/*m_activeViewInfo*/;
                m_viewInfoMng.Remove(vim);
                m_viewInfoMng.Insert(m_DestinationIndex, vim);

                ChangeTab(GetActiveTabIndex());

                RefreshTabPanel();

                dragFlag = false;

                return;
            }

            TextEditor te;
            int ret = m_MainApp.isPointClientArea(sp, out te);
            if (dragFlag && ret == 1)
            {
                // アクティブフォーム

                // 何もしない
                ;
            }
            else if(dragFlag && ret == 2)
            {
                // 他ビュー

                // アクティブビュー移動
                MoveActiveView(te);
            }
            else if(dragFlag && ret == 3)
            {
                // その他
                ViewNew_Click(null, EventArgs.Empty);
            }

            dragFlag = false;
        }

        /// <summary>
        /// タブボタン押下イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabButton_MouseDown(object sender, MouseEventArgs e)
        {
            // マウス左クリック以外無効
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            int idx = 0;
            for (; idx < m_viewInfoMng.Count; idx++)
            {
                if (sender.Equals(m_viewInfoMng[idx].button))
                {
                    break;
                }
            }

            // タブ切替
            if (m_viewInfoMng.Count > idx && m_viewInfoMng[idx].active == false)
            {
                ChangeTab(idx);
            }

            dragFlag = true;
        }

        /// <summary>
        /// タブボタン移動イベント
        /// </summary>
        int m_DestinationIndex = -1;    // 移動先インデックス
        int bkDestinationIndex = -1;    // 移動先インデックス保存(再描画抑止用)
        private void TabButton_MouseMove(object sender, MouseEventArgs e)
        {
            // マウス左クリック以外無効
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (!dragFlag) return;

            Point sp = Cursor.Position;

            m_DestinationIndex = -1;

            // クライアント外の場合はポインタを変更
            TextEditor textEditor;
            int ret = m_MainApp.isPointClientArea(sp, out textEditor);
            if (ret == 1)
            {
                Point cp = this.PointToClient(sp);

                if (tabPanel.Left < cp.X && cp.X < tabPanel.Left + tabPanel.Width && cp.Y > tabPanel.Top && cp.Y < tabPanel.Top + tabPanel.Height)
                {
                    // アクティブフォーム内のタブパネルの場合はカーソルを変更しない
                    //Cursor.Current = Cursors.Default;

                    // 移動先位置（インデックス）取得
                    if ((m_viewInfoMng.Count * CEConstants.TabWidth) - (CEConstants.TabWidth / 2) <= cp.X)
                    {
                        // 後ろの方
                        m_DestinationIndex = m_viewInfoMng.Count - 1;
                    }
                    else if (0 <= cp.X && cp.X < CEConstants.TabWidth / 2)
                    {
                        // 前の方
                        m_DestinationIndex = 0;
                    }
                    else
                    {
                        // 中間
                        double d = cp.X / (CEConstants.TabWidth / 2);
                        m_DestinationIndex = (int)Math.Ceiling(d / 2);
                        // 現在位置よりも右側の場合、現在の位置のタブをカウントしない為-1する
                        if (GetActiveViewIndex()/*m_activeViewIndex*/ < m_DestinationIndex )
                        {
                            m_DestinationIndex--;
                        }
                    }
                    Cursor.Current = Cursors.Default;

                    // タブ挿入位置に赤線を引く
                    if (bkDestinationIndex != m_DestinationIndex)
                    {
                        tabPanel.Refresh();
                        Graphics g = tabPanel.CreateGraphics();
                        int x = m_DestinationIndex * CEConstants.TabWidth;
                        if (GetActiveViewIndex()/*m_activeViewIndex*/ < m_DestinationIndex)
                        {
                            x += CEConstants.TabWidth;
                        }
                        g.FillRectangle(Brushes.Red/*.Black*/, x - 2, 0, 5, tabPanel.Height);
                    }
                    bkDestinationIndex = m_DestinationIndex;
                }
                else
                {
                    // アクティブフォーム内のタブパネル外の場合は操作不可カーソルに変更する
                    Cursor.Current = Cursors.No;
                }
            }
            else if (ret == 2)
            {
                // 他フォーム
                Cursor.Current = Cursors.Hand;
            }
            else if (ret == 3)
            {
                // その他
                Cursor.Current = Cursors.PanNW;
            }
        }

        /// <summary>
        /// タブボタンに入力すると発生するイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabButton_MouseEnter(object sender, EventArgs e)
        {
            Control[] cl = (sender as TabButton).Controls.Find("closeButton", true);
            if (cl.Length > 0)
            {
                (cl[0] as Label).Text = "×";
            }
            //((TabButton)sender).BackColor = Color.FromArgb(255, ColorTranslator.FromHtml(CECommon.ChgRGB(CEConstants.TabBackColor).ToString()));
        }

        /// <summary>
        /// タブボタンから離れると発生するイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabButton_MouseLeave(object sender, EventArgs e)
        {
            Control[] cl = (sender as TabButton).Controls.Find("closeButton", true);
            if (cl.Length > 0)
            {
                (cl[0] as Label).Text = "";
            }
        }

        /// <summary>
        /// タブクローズボタンに入力すると発生するイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButton_MouseEnter(object sender, EventArgs e)
        {
            (sender as Label).Text = "×";
            (sender as Label).ForeColor = Color.Red;
        }

        /// <summary>
        /// タブクローズボタンから離れると発生するイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButton_MouseLeave(object sender, EventArgs e)
        {
            (sender as Label).Text = "";
            (sender as Label).ForeColor = Color.Black;
        }

        /// <summary>
        /// 新規作成イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void NewMenuItem_Click(object sender, EventArgs e)
        {
            // 新規ビュー作成
            NewTabView();

            // タブパネル再描画
            RefreshTabPanel();
        }

        /// <summary>
        /// ビュー分離
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewNew_Click(object sender, EventArgs e)
        {
            // ビューが一つしかない場合、何もしない
            if (m_viewInfoMng.Count == 1) return;

            // 分離するビューの情報取得
            ViewInfoMng vim = GetActiveViewInfo()/*m_activeViewInfo*/;

            // 分離するビューの右隣のインデックス取得
            int activeTabIndex = GetActiveTabIndex() - 1;
            activeTabIndex = activeTabIndex < 0 ? 0 : activeTabIndex;

            // ビューを表示するフォームを移動するためイベント削除
            // イベントを削除しておかないと分離元のフォームにイベントが飛んでくるため
            vim.customTextBox.CaretPos -= new CEEditView.CaretPositionEventHandler(this.CaretPosition);

            // ビュー管理情報から分離するビュー情報を削除
            m_viewInfoMng.Remove(vim);

            // ボタンを削除
            vim.button.Dispose();

            // ビューが一つ無くなったのでタブパネル再描画
            ChangeTab(activeTabIndex);
            RefreshTabPanel();

            // 
            //m_MainApp.TabViewSeparation(vim.customTextBox, vim.fileName);
            m_MainApp.TabViewSeparation(vim.customTextBox, vim.button.openFilePath);
#if false
            // MainAppへビュー情報を渡す（デリゲート）
            ViewMove(new ViewMoveEventArgs(vim.customTextBox, vim.fileName));
#endif
        }

        /// <summary>
        /// アクティブビュー(タブ)移動
        /// </summary>
        /// <param name="te"></param>
        private void MoveActiveView(TextEditor te)
        {
            // ビューが一つしかない場合、何もしない
            //if (m_viewInfoMng.Count == 1) return;

            // 分離するビューの情報取得
            ViewInfoMng vim = GetActiveViewInfo()/*m_activeViewInfo*/;

            // 分離するビューの右隣のインデックス取得
            int activeTabIndex = GetActiveTabIndex() - 1;
            activeTabIndex = activeTabIndex < 0 ? 0 : activeTabIndex;

            // ビューを表示するフォームを移動するためイベント削除
            // イベントを削除しておかないと分離元のフォームにイベントが飛んでくるため
            vim.customTextBox.CaretPos -= new CEEditView.CaretPositionEventHandler(this.CaretPosition);

            // ビュー管理情報から分離するビュー情報を削除
            m_viewInfoMng.Remove(vim);

            // ボタンを削除
            vim.button.Dispose();

            // 別ウィンドウにタブビュー移動
            //m_MainApp.TabViewAnotherWinMove(te, vim.customTextBox, vim.fileName);
            m_MainApp.TabViewAnotherWinMove(te, vim.customTextBox, vim.button.openFilePath);

            if (m_viewInfoMng.Count == 0)
            {
                // ビューが亡くなったので閉じる
                this.Close();
            }
            else
            {
                // ビューが一つ無くなったのでタブパネル再描画
                ChangeTab(activeTabIndex);
                RefreshTabPanel();
            }
        }

        // --- >>> 開く >>> ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// ファイルを開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.FileName = "default.html";
            ofd.InitialDirectory = @"C:\";
            ofd.Filter = "テキストファイル(*.txt)|*.txt|すべてのファイル(*.*)|*.*";
            ofd.FilterIndex = 2;
            ofd.Title = "開くファイルを選択してください";
            ofd.RestoreDirectory = true;
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                ViewInfoMng vim = GetActiveViewInfo();
                OpenFile(vim/*m_activeViewInfo*/.customTextBox, ofd.FileName);
                vim/*m_activeViewInfo*/.customTextBox.Refresh();

                // タブにファイル名表示
                SetButtonName(Path.GetFileName(ofd.FileName));

                // タブボタンに開いているファイル名を保存する
                vim/*m_activeViewInfo*/.button.openFilePath = ofd.FileName;

                // ツールチップ設定
                ToolTip ToolTip1 = new ToolTip();
                ToolTip1.SetToolTip(m_viewInfoMng[m_viewInfoMng.Count - 1].button, ofd.FileName);

                // タブパネルを再描画
                RefreshTabPanel();
            }
        }

        /// <summary>
        /// DragDropイベント
        /// ドロップされたとき
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DragDropEvent(object sender, DragEventArgs e)
        {
            string[] dropData = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            // ドロップされたファイル分繰り返し、オープンし表示する
            ViewInfoMng vim = GetActiveViewInfo();
            for (int idx = 0; idx < dropData.Length; idx++)
            {
                string fileName = dropData[idx];

#if false
                if (m_activeViewInfo.customTextBox.IsTextData())
#else
                if (vim/*m_activeViewInfo*/.fileName != "")
#endif
                {
                    // データあり

                    OpenTabView(fileName);
                }
                else
                {
                    // データなし

                    // ファイルオープン
                    OpenFile(vim/*m_activeViewInfo*/.customTextBox, fileName);

                    // 再描画
                    vim/*m_activeViewInfo*/.customTextBox.Refresh();

                    // タブにファイル名表示
                    SetButtonName(Path.GetFileName(fileName));

                    // タブボタンに開いているファイル名を保存する
                    vim/*m_activeViewInfo*/.button.openFilePath = fileName;

                    // ツールチップ設定
                    ToolTip ToolTip1 = new ToolTip();
                    ToolTip1.SetToolTip(m_viewInfoMng[m_viewInfoMng.Count - 1].button, fileName);
                }
            }

            // このフォームをアクティブ
            this.Activate();

            // タブパネルを再描画
            RefreshTabPanel();
        }

        // --- <<< 開く <<< ---------------------------------------------------------------------------------------------------------------------

        // --- >>> 保存 >>> ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 名前をつけて保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAsMenuItem_Click(object sender, EventArgs e)
        {
            SveAsFileDialog();
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveMenuItem_Click(object sender, EventArgs e)
        {
            ViewInfoMng vim = GetActiveViewInfo();
            // 新規ファイルかチェック
            if (vim/*m_activeViewInfo*/.fileName ==  "")
            {
                // 新規ファイルの場合は保存ダイアログを表示
                SveAsFileDialog();
            }
            else
            {
                // 既存ファイルの場合はそのまま保存
                SaveFile(vim/*m_activeViewInfo*/.fileName);
            }
        }

        // --- <<< 保存 <<< ---------------------------------------------------------------------------------------------------------------------

        // --- >>> 閉じる >>> ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// ビューを閉じるイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewClose_Click(object sender, EventArgs e)
        {
            ViewInfoMng vim = GetActiveViewInfo();

            // タブビュー保存
            // キャンセルした場合は何もしない
            if (!this.CloseTabView(vim/*m_activeViewInfo*/)) return;

            // 閉じるビューの右隣のインデックス取得
            int activeTabIndex = GetActiveTabIndex() - 1;
            activeTabIndex = activeTabIndex < 0 ? 0 : activeTabIndex;

            // ビュー管理情報から削除
            vim/*m_activeViewInfo*/.button.Dispose();
            vim/*m_activeViewInfo*/.customTextBox.Dispose();
            m_viewInfoMng.Remove(vim/*m_activeViewInfo*/);

            if (m_viewInfoMng.Count == 0)
            {
                // タブが無くなった場合、新規に作成

                // 新規ビュー作成
                NewEditView(new CEEditView());

                // タブ切替
                ChangeTab(0);
            }
            else
            {
                // タブ再描画
                ChangeTab(activeTabIndex);
                RefreshTabPanel();
            }
        }

        /// <summary>
        /// フォームを閉じるときに発生
        /// ファイルを閉じる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormClosingEvent(object sender, FormClosingEventArgs e)
        {
            int num = m_viewInfoMng.Count;
            for (int idx = num - 1; idx >= 0; idx--)
            {
                ChangeTab(idx);
                if (this.CloseTabView(m_viewInfoMng[idx]))
                {
                    // ビュー管理情報から削除
                    m_viewInfoMng[idx].button.Dispose();
                    m_viewInfoMng[idx].customTextBox.Dispose();
                    m_viewInfoMng.Remove(m_viewInfoMng[idx]);

                    if (m_viewInfoMng.Count != 0)
                    {
                        // タブ再描画
                        RefreshTabPanel();
                    }
                }
            }

            // まだタブビューが残っている場合はウィンドウをクローズしない
            if (m_viewInfoMng.Count != 0)
            {
                e.Cancel = true;
            }
        }

        // --- <<< 閉じる <<< ---------------------------------------------------------------------------------------------------------------------

        // --- >>> その他 >>> ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// コピーイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyMenuItem_Click(object sender, EventArgs e)
        {
#if false // 検索パネル未使用
            if (!m_activeViewInfo.findPanel.findComboBox.Focused)
            {
                m_activeViewInfo.customTextBox.CopyData();
            }
            else
            {
                m_activeViewInfo.findPanel.CopyData();
            }
#else
                GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.CopyData();
#endif
        }

        /// <summary>
        /// ペーストイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PasteMenuItem_Click(object sender, EventArgs e)
        {
#if false // 検索パネル未使用
            // ペーストしたら矩形選択メニューを解除
            //RectSelectToolStripMenuItem.Checked = false;
            if (!m_activeViewInfo.findPanel.findComboBox.Focused)
            {
                m_activeViewInfo.customTextBox.PasteData();
            }
            else
            {
                m_activeViewInfo.findPanel.PasetData();
            }
#else
            GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.PasteData();
#endif
        }

        /// <summary>
        /// カットイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CutMenuItem_Click(object sender, EventArgs e)
        {
#if false // 検索パネル未使用
            if (!m_activeViewInfo.findPanel.findComboBox.Focused)
            {
                m_activeViewInfo.customTextBox.CutData();
            }
            else
            {
                m_activeViewInfo.findPanel.CutData();
            }
#else
            GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.CutData();
#endif
        }

        /// <summary>
        /// 閉じるイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            // FormClosingイベントが呼ばれる
            this.Close();
        }

        /// <summary>
        /// DragEnterイベント
        /// ドラッグされたとき
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DragEnterEvent(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        /// <summary>
        /// 全てを選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AllSelectMenuItem_Click(object sender, EventArgs e)
        {
            GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.AllSelect();
        }

        /// <summary>
        /// 元に戻す（Undo）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolStripMenuItem_Undo_Click(object sender, EventArgs e)
        {
            // アンドゥ
            GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.Undo();
        }

        /// <summary>
        /// やり直し（Redo）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolStripMenuItem_Redo_Click(object sender, EventArgs e)
        {
            // リドゥ
            GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.Redo();
        }

        /// <summary>
        /// エンコード選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EncodeSelect(object sender, ToolStripItemClickedEventArgs e)
        {
            toolStripDropDownBtn.Text = e.ClickedItem.ToString();
        }

        /// <summary>
        /// ヘルプ（あいさつ）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StripMenuItem_Help_Click(object sender, EventArgs e)
        {
            Help h = new Help();
            h.Show();
        }

        /// <summary>
        /// 検索パネル
        /// </summary>
        //Panel m_findPanel;

        /// <summary>
        /// 検索ダイアログ表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StripMenuItem_Find_Click(object sender, EventArgs e)
        {
            ViewInfoMng vim = GetActiveViewInfo();

            CEEditView targetView = vim/*m_activeViewInfo*/.customTextBox;
#if false // 検索パネル未使用
            // 検索パネル
            if (m_activeViewInfo.findPanel == null)
            {
                m_activeViewInfo.findPanel = new FindPanel(targetView);
                Size vScrollbarSize = targetView.GetVScrollbarSize(); // 縦スクロールバーサイズ
                m_activeViewInfo.findPanel.Location = new Point(this.ClientSize.Width - m_activeViewInfo.findPanel.Width - vScrollbarSize.Width, 0);

                m_activeViewInfo.findPanel.Visible = true;
                targetView.Controls.Add(m_activeViewInfo.findPanel);
                m_activeViewInfo.findPanel.Anchor = AnchorStyles.Right | AnchorStyles.Top; // 右上に固定
                m_activeViewInfo.findPanel.BringToFront(); // 前面に表示
            }

            // 検索パネル表示
            m_activeViewInfo.findPanel.Visible = true;

            m_activeViewInfo.findPanel.findComboBox.Focus();
#endif
#if true // 検索ダイアログ未使用
            // 検索ダイアログ表示
           fd = new FindDlg(vim/*m_activeViewInfo*/.customTextBox);

            fd.FormClosed += new FormClosedEventHandler(findFormClosed);
            fd.ShowInTaskbar = false;
            fd.Show(this);

            Size vScrollbarSize = targetView.GetVScrollbarSize(); // 縦スクロールバーサイズ
            Point cp = this.PointToScreen(new Point(this.ClientSize.Width - fd.Width - vScrollbarSize.Width, menuStrip1.Height + tabPanel.Height));
            fd.Location = cp;
#endif
        }
#if true // 検索ダイアログ未使用
        /// <summary>
        /// 検索ダイアログ終了時イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void findFormClosed(object sender, FormClosedEventArgs e)
        {
            // 検索条件保存
            FindDlg f2 = (FindDlg)sender;
            findString = f2.findString;
            m_ul = f2.ulToggle;
            m_word = f2.wordToggle;
            m_reg = f2.regToggle;
        }
#endif
        /// <summary>
        /// 次を検索
        /// メニューから次を検索を選択したら呼ばれる（F3でも呼ばれる）
        /// </summary>
        /// <param name="s">検索文字列</param>
        /// <param name="b">大文字／小文字区分</param>
        private void StripMenuItem_NextFind_Click(object sender, EventArgs e)
        {
#if false // 検索パネル未使用
            if (m_activeViewInfo == null || m_activeViewInfo.findPanel == null)
            {
                return;
            }

            m_activeViewInfo.findPanel.NextFind();
#else
            GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.NextFind(findString, m_ul, m_word, m_reg);
#endif
        }

        /// <summary>
        /// 前を検索
        /// メニューから前を検索を選択したら呼ばれる（Shift+F3でも呼ばれる）
        /// </summary>
        /// <param name="s">検索文字列</param>
        /// <param name="b">大文字／小文字区分</param>
        private void StripMenuItem_PrevFind_Click(object sender, EventArgs e)
        {
#if false // 検索パネル未使用
            if (m_activeViewInfo == null || m_activeViewInfo.findPanel == null)
            {
                return;
            }

            m_activeViewInfo.findPanel.PrevFind();
#else
            GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.PrevFind(findString, m_ul, m_word, m_reg);
#endif
        }

        /// <summary>
        /// フリーカーソル
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FreeCursorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            item.Checked = !item.Checked;
            GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.SetCursorType(item.Checked);
        }

        /// <summary>
        /// 右端で折り返し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RightWrapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            item.Checked = !item.Checked;
            GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.SetWrap(item.Checked);
        }

        /// <summary>
        /// 矩形選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RectSelectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.SetRectangle(true);
        }

        /// <summary>
        /// バージョン情報
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VersionInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VersionInfo fd = new VersionInfo();
            fd.Show(this);
        }

        /// <summary>
        /// フォント設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ViewInfoMng vim = GetActiveViewInfo();

            // フォント表示
            FontDialog fd = new FontDialog();
            fd.Font = vim/*m_activeViewInfo*/.customTextBox.GetFont();            // 初期のフォントを設定
            fd.Color = Color.FromArgb(255, 255, 255, 255);  // 初期の色を設定
            fd.MaxSize = 24;                                // 最大選択フォントサイズ
            fd.MinSize = 9;                                 // 最少選択フォントサイズ
            fd.FontMustExist = true;                        // 存在しないフォント等を指定した場合エラー表示
            fd.AllowVerticalFonts = false;                  // 横書きフォントのみ表示
            fd.ShowColor = false;                           // 色を選択できないようにする
            fd.ShowEffects = false;                         // 取消線、下線等を選択できないようにする
            fd.FixedPitchOnly = true;                       // 固定ピッチフォントのみ表示
            fd.AllowVectorFonts = false;                    // ベクタフォントを選択できないようにする
            if (fd.ShowDialog() != DialogResult.Cancel)
            {
                // フォント設定
                vim/*m_activeViewInfo*/.customTextBox.SetFont(fd.Font);

                // 再描画
                vim/*m_activeViewInfo*/.customTextBox.Refresh();
            }
        }

        /// <summary>
        /// フォームが初めて表示された場合のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShownEvent(object sender, EventArgs e)
        {
            if ((this.openFilePath != null) && (this.openFilePath != ""))
            {
                OpenTabView(this.openFilePath);

                openFilePath = "";
#if false
                // 新規ビュー作成
                NewEditView(new CEEditView());

                // タブ切替
                changeTab(m_viewInfoMng.Count - 1);

                // ファイルオープン
                OpenFile(m_activeViewInfo.customTextBox, this.openFilePath);

                // タブにファイル名表示
                SetButtonName(Path.GetFileName(this.openFilePath));

                // 再描画
                m_activeViewInfo.customTextBox.Refresh();
#endif
            }
        }

        /// <summary>
        /// タブパネル描画
        /// パネルに下線を引く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabPanelPaintEvent(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            CEWin32Api.RECT rc;
            CEWin32Api.GetClientRect(this.Handle, out rc);

            SolidBrush p = new SolidBrush(Color.Black);
            g.DrawLine(Pens.Black, 0, tabPanel.Height -1, rc.right, tabPanel.Height - 1);

            // 新規タブボタンの枠表示
            //g.FillRectangle(Brushes.Black, m_viewInfoMng.Count * CEConstants.TabWidth - 3, 4, 17, 17);
            g.DrawRectangle(Pens.Black, m_viewInfoMng.Count * CEConstants.TabWidth - 3, 1, 20, 20);

            p.Dispose();
            g.Dispose();
        }

        // --- <<< その他 <<< ---------------------------------------------------------------------------------------------------------------------

#endregion

        /// <summary>
        /// タブビューを閉じる
        /// </summary>
        /// <param name="vim"></param>
        /// <returns>true:閉じる / false:キャンセル</returns>
        private Boolean CloseTabView(ViewInfoMng vim)
        {
            Boolean ret = false;

            // テキストデータがありかつ編集されている場合
            if (vim.customTextBox.GetEditStatus() && vim.customTextBox.IsTextData())
            {
                // 編集中

                // メッセージボックスを表示する
                DialogResult result = MessageBox.Show("ファイルが編集されています。保存しますか？",
                    "確認",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button2);

                if (result == DialogResult.Yes)
                {
                    // 保存

                    if (vim.fileName == "")
                    {
                        // 新規ファイル

                        if (!SveAsFileDialog())
                        {
                            // 保存をキャンセルしたので閉じない
                            ret = false;
                        }
                    }
                    else
                    {
                        // 保存済み編集中ファイル

                        SaveFile(GetActiveViewInfo()/*m_activeViewInfo*/.fileName);
                        ret = true;
                    }
                }
                else if (result == DialogResult.No)
                {
                    // 保存せずに閉じる
                    ret = true;
                }
                else if (result == DialogResult.Cancel)
                {
                    // キャンセルなので閉じない
                    ret = false;
                }
            }
            else
            {
                ret = true;
            }

            return ret;
        }

        /// <summary>
        /// ポインタがクライアントないかチェック
        /// </summary>
        /// <returns>true:クライアント内／false:クライアント外</returns>
        public Boolean isPointClientArea()
        {
            ViewInfoMng vim = GetActiveViewInfo();

            Point sp = Cursor.Position;
            Point cp = this.PointToClient(sp);

            if (cp.X < 0 || cp.Y < 0 || cp.X > vim/*m_activeViewInfo*/.customTextBox.Width || cp.Y > vim/*m_activeViewInfo*/.customTextBox.Height + tabPanel.Height + menuStrip1.Height + StatusBar.Height)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 指定のタブに切替
        /// </summary>
        /// <param name="idx">切り換えるタブ番号</param>
        public void ChangeTab(int idx)
        {
            if (m_viewInfoMng.Count <= idx)
            {
                return;
            }

            // アクティブ情報更新の前に、現在表示中のビューを非表示
            //HideCurrentTextBox();

            // アクティブ情報更新
            m_viewInfoMng.ForEach(x => x.active = false);
            m_viewInfoMng[idx].active = true;

            // 表示するビューを最前面
            m_viewInfoMng[idx].customTextBox.BringToFront();

            // タブ色設定
            m_viewInfoMng.ForEach(view =>
            {
                if (view.active)
                {
                    // アクティブ
                    view.button.BackColor = Color.FromArgb(255, ColorTranslator.FromHtml(CECommon.ChgRGB(CEConstants.TabBackColor).ToString()));
                }
                else
                {
                    // 非アクティブ
                    //view.button.BackColor = Color.Gainsboro;
                    view.button.BackColor = SystemColors.Control;
                }
            });

            // 表示するビューをアクティブ
            m_viewInfoMng[idx].customTextBox.Show();
            m_viewInfoMng[idx].customTextBox.Focus();

            m_viewInfoMng.ForEach(x => x.button.m_active = false);
            m_viewInfoMng[idx].button.m_active = true;
        }

#if false // 未使用。当初非表示にしないとうまく切り替わらなかったためhideしていたが、今はなくてもうまく切り替わるようになったためコメントアウト。
        /// <summary>
        /// 現在表示中のビューを非表示
        /// </summary>
        private void HideCurrentTextBox()
        {
#if false
            if (m_activeViewInfo != null)
            {
                m_activeViewInfo.customTextBox.Hide();
            }
#else
            for (int idx = 0; idx < m_viewInfoMng.Count; idx++)
            {
                m_viewInfoMng[idx].customTextBox.Hide();
            }
#endif
        }
#endif

        /// <summary>
        /// アクティブタブに指定された文字列を表示
        /// </summary>
        /// <param name="str">表示文字列</param>
        private void SetButtonName(string str)
        {
            ViewInfoMng vim = GetActiveViewInfo();

            if (vim/*m_activeViewInfo*/ != null)
            {
                vim/*m_activeViewInfo*/.button.Text = str;
            }
        }

        /// <summary>
        /// タブパネルの再描画
        /// </summary>
        /// <param name="activeIdx"></param>
        private void RefreshTabPanel()
        {
            // タブボタン
            for (int idx = 0; idx < m_viewInfoMng.Count; idx++)
            {
                m_viewInfoMng[idx].button.Location = new Point((idx * CEConstants.TabWidth) + 2, 0);
            }

            // 新規タブボタン
            m_addTabButton.Location = new Point(m_viewInfoMng.Count * CEConstants.TabWidth, 6);

            // 閉じるボタン
            m_closeTabButton.Location = new Point(tabPanel.Width - m_closeTabButton.Size.Width - 3, 6);

            // タブ再描画
            tabPanel.Refresh();
        }

        /// <summary>
        /// ファイルを開く
        /// </summary>
        /// <param name="file"></param>
        public void OpenTabView(string file)
        {
            // 新規ビュー作成
            NewEditView(new CEEditView());

            // タブ切替
            ChangeTab(m_viewInfoMng.Count - 1);

            ViewInfoMng vim = GetActiveViewInfo();

            // ファイルオープン
            OpenFile(vim/*m_activeViewInfo*/.customTextBox, file);

            // 再描画
            vim/*m_activeViewInfo*/.customTextBox.Refresh();

            // タブにファイル名表示
            SetButtonName(Path.GetFileName(file));

            // タブボタンに開いているファイル名を保存する
            vim/*m_activeViewInfo*/.button.openFilePath = file;

            // ツールチップ設定
            ToolTip ToolTip1 = new ToolTip();
            ToolTip1.SetToolTip(m_viewInfoMng[m_viewInfoMng.Count - 1].button, file);

            // タブ追加ボタン追加
            AddTabButton();

            // タブパネルを再描画
            RefreshTabPanel();
        }

        /// <summary>
        /// ビュー作成
        /// </summary>
        public void NewTabView()
        {
            // 新規ビュー作成
            NewEditView(new CEEditView());

            // タブ切替
            ChangeTab(m_viewInfoMng.Count - 1);

            // ツールチップ設定
            ToolTip ToolTip1 = new ToolTip();
            ToolTip1.SetToolTip(m_viewInfoMng[m_viewInfoMng.Count - 1].button, CEConstants.EmptyViewName);
        }

        /// <summary>
        /// ビュー追加
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="fn"></param>
        public void AddTabView(CEEditView ev, string fn)
        {
            NewEditView(ev);
            m_viewInfoMng[m_viewInfoMng.Count - 1].fileName = fn;
            m_viewInfoMng[m_viewInfoMng.Count - 1].active = true;

            // タブ切替
            ChangeTab(m_viewInfoMng.Count - 1);

            ViewInfoMng vim = GetActiveViewInfo();

            // 再描画
            vim/*m_activeViewInfo*/.customTextBox.Refresh();

            // タブにファイル名表示
            SetButtonName(Path.GetFileName(fn));

            // タブボタンに開いているファイル名を保存する
            vim/*m_activeViewInfo*/.button.openFilePath = fn;

            // ツールチップ設定
            ToolTip ToolTip1 = new ToolTip();
            ToolTip1.SetToolTip(m_viewInfoMng[m_viewInfoMng.Count - 1].button, fn);

            // タブ追加ボタン追加
            AddTabButton();

            // 再描画
            RefreshTabPanel();
        }

        /// <summary>
        /// アクティブタブのインデックス取得
        /// </summary>
        /// <remarks>
        /// アクティブタブが設定されていない場合は先頭（０）を返す。
        /// </remarks>
        /// <returns>アクティブタブのインデックス</returns>
        private int GetActiveTabIndex()
        {
            int idx = 0;
            for (idx = 0; idx < m_viewInfoMng.Count; idx++)
            {
                if (m_viewInfoMng[idx].active)
                {
                    break;
                }
            }

            return idx;
        }

        /// <summary>
        /// 新規ビュー及びタブ作成
        /// </summary>
        /// <param name="idx"></param>
        private void NewEditView(CEEditView editView)
        {
            ViewInfoMng vi = new ViewInfoMng();

            // コンテキストメニュー
            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = new MenuItem();
            MenuItem menuItem2 = new MenuItem();
            contextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] { menuItem, menuItem2 });
            menuItem.Index = 0;
            menuItem.Text = "タブを閉じる";
            menuItem.Click += new EventHandler(ViewClose_Click);
            menuItem2.Index = 0;
            menuItem2.Text = "タブ分離";
            menuItem2.Click += new EventHandler(ViewNew_Click);

            // タブ
            vi.button = new TabButton();
            //vi.button.Text = "[無題]";
            vi.button.openFilePath = CEConstants.EmptyViewName;
            //vi.button.TextAlign = ContentAlignment.MiddleLeft;
            //vi.button.AutoSize = false;
            vi.button.Size = new Size(CEConstants.TabWidth - 4, tabPanel.Height - 0);
            vi.button.Location = new Point(((m_viewInfoMng == null ? 0 : m_viewInfoMng.Count) * CEConstants.TabWidth) + 2, 0);
            vi.button.MouseUp += new MouseEventHandler(TabButton_MouseUp);
            vi.button.MouseDown += new MouseEventHandler(TabButton_MouseDown);
            vi.button.MouseMove += new MouseEventHandler(TabButton_MouseMove);
            vi.button.MouseEnter += new EventHandler(TabButton_MouseEnter);
            vi.button.MouseLeave += new EventHandler(TabButton_MouseLeave);
            //vi.button.FlatStyle = FlatStyle.Flat;
            //vi.button.FlatAppearance.BorderSize = 1;
            //vi.button.FlatAppearance.BorderColor = Color.CadetBlue;
            //this.SetStyle(ControlStyles.Selectable, false);
            vi.button.ForeColor = Color.FromArgb(255, ColorTranslator.FromHtml(CECommon.ChgRGB(CEConstants.TabFontColor).ToString()));
            vi.button.Font = new Font("ＭＳ ゴシック", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(128)));
            vi.button.ContextMenu = contextMenu; // ボタンにコンテキストメニューを追加
            Label closeButton = new Label();
            closeButton.Text = "";
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(25, 25);
            closeButton.Location = new Point(vi.button.Width - 18, 5);
            closeButton.Visible = true;
            closeButton.Click += new EventHandler(ViewClose_Click);
            closeButton.MouseEnter += new EventHandler(CloseButton_MouseEnter);
            closeButton.MouseLeave += new EventHandler(CloseButton_MouseLeave);
            closeButton.BackColor = Color.Transparent;
            closeButton.ForeColor = Color.Black;
            //closeButton.Font = new Font(closeButton.Font, FontStyle.Bold);
            closeButton.Font = new Font("ＭＳ ゴシック", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(128)));
            vi.button.Controls.Add(closeButton);
            tabPanel.Controls.Add(vi.button);

            // ビュー
            vi.customTextBox = editView;
            vi.customTextBox.Anchor = (((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            vi.customTextBox.BackColor = Color.White;
            vi.customTextBox.Cursor = Cursors.IBeam;
            vi.customTextBox.Font = new Font("ＭＳ ゴシック", 10F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(128)));
            vi.customTextBox.ForeColor = Color.Black;
            vi.customTextBox.Location = new Point(0, 47);
            vi.customTextBox.Size = new Size(658, 509);
            vi.customTextBox.TabIndex = 5;

            // キャレット位置通知用イベント登録
            vi.customTextBox.CaretPos += new CEEditView.CaretPositionEventHandler(this.CaretPosition);

            // カスタムテキストボックスコントロールをフォームクライアントに合わせる
            vi.customTextBox.Location = new Point(0, menuStrip1.Height + tabPanel.Height);
            vi.customTextBox.Height = this.ClientSize.Height - menuStrip1.Height - tabPanel.Height - StatusBar.Height;
            vi.customTextBox.Width = this.ClientSize.Width;
            Controls.Add(vi.customTextBox);

            // ビュー管理情報に作成したビューとタグを追加
            m_viewInfoMng.Add(vi);
        }

        /// <summary>
        /// タブ追加ボタン
        /// </summary>
        Label m_addTabButton;
        public void AddTabButton()
        {
            // 「タブ追加」ボタン名称
            string addTabButtonName = "addTabButton";

            // 「タブパネル」に追加されている場合は何もしない
            if (tabPanel.Controls.Find(addTabButtonName, true).Length > 0)
            {
                return;
            }

            m_addTabButton = new Label();
            m_addTabButton.Name = addTabButtonName;
            m_addTabButton.Size = new Size(13, 13);
            m_addTabButton.Location = new Point(m_viewInfoMng.Count * CEConstants.TabWidth, 6);
            m_addTabButton.ForeColor = Color.FromArgb(255, ColorTranslator.FromHtml(CECommon.ChgRGB(CEConstants.TabFontColor).ToString()));
            m_addTabButton.Font = new Font("ＭＳ ゴシック", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(128)));
            m_addTabButton.Click += new EventHandler(NewMenuItem_Click);
            m_addTabButton.BackColor = Color.Transparent;
            m_addTabButton.ForeColor = Color.Black;
            m_addTabButton.Text = "＋";
            tabPanel.Controls.Add(m_addTabButton);
        }

        // ビュー閉じるボタン追加
        Label m_closeTabButton;
        private void AddViewCloseButton()
        {
            m_closeTabButton = new Label();
            m_closeTabButton.Size = new Size(13, 13);
            m_closeTabButton.Location = new Point(tabPanel.Width - m_closeTabButton.Size.Width - 3, 6);
            m_closeTabButton.ForeColor = Color.FromArgb(255, ColorTranslator.FromHtml(CECommon.ChgRGB(CEConstants.TabFontColor).ToString()));
            m_closeTabButton.Font = new Font("ＭＳ ゴシック", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(128)));
            m_closeTabButton.Click += new EventHandler(ViewClose_Click);
            m_closeTabButton.BackColor = Color.Transparent;
            m_closeTabButton.ForeColor = Color.Black;
            m_closeTabButton.Text = "×";
            m_closeTabButton.Anchor = AnchorStyles.Right;
            tabPanel.Controls.Add(m_closeTabButton);
        }

        /// <summary>
        /// 指定されたファイルをオープン
        /// </summary>
        /// <param name="openFilePath"></param>
        private void OpenFile(CEEditView obj, string openFilePath)
        {
            // ファイル読み込み
            if (!obj.ReadFile(openFilePath, out m_encode))
            {
                // アクティブ
                this.Activate();
                // 文字コードの検出失敗
                MessageBox.Show("指定されたファイルの文字コードの検出に失敗しました。");

                // 読込失敗
                return;
            }

            // ステータスバーに文字コードを表示
            toolStripDropDownBtn.Text = m_encode.EncodingName;

            ViewInfoMng vim = GetActiveViewInfo();

            // 読込んだファイル名保存
            vim/*m_activeViewInfo*/.fileName = openFilePath;

            // タイトルにファイル名表示
            this.Text = vim/*m_activeViewInfo*/.fileName;
        }

        /// <summary>
        /// 保存ダイアログ
        /// </summary>
        /// <returns>true:保存、false:キャンセル</returns>
        private Boolean SveAsFileDialog()
        {
            Boolean ret = false;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "新しいファイル.txt";
            string stCurrentDir = System.IO.Directory.GetCurrentDirectory();
            sfd.InitialDirectory = stCurrentDir;
            sfd.Filter = "テキストファイル(*.txt)|*.txt|すべてのファイル(*.*)|*.*";
            sfd.FilterIndex = 2;
            sfd.Title = "保存先のファイルを選択してください";
            sfd.RestoreDirectory = true;
            sfd.OverwritePrompt = true;
            sfd.CheckPathExists = true;

            //ダイアログを表示する
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                SaveFile(sfd.FileName);
                GetActiveViewInfo()/*m_activeViewInfo*/.fileName = sfd.FileName;
                ret = true;
            }

            return ret;
        }

        /// <summary>
        /// ファイルの保存
        /// </summary>
        /// <param name="fileName">保存ファイル名（フルパス指定）</param>
        private void SaveFile(string fileName)
        {
            // 選択されているエンコードを取得
            foreach (Encoding e in m_selectEncode)
            {
                if (e.EncodingName == toolStripDropDownBtn.Text)
                {
                    m_encode = e;
                    break;
                }
            }

            // ファイル書込
            GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.WriteFile(fileName, m_encode);

            // ステータスバーに文字コードを表示
            toolStripDropDownBtn.Text = m_encode.EncodingName;
#if false
            // 書き込んだファイル名保存
            m_fileName = fileName;

            // タイトルにファイル名表示
            this.Text = m_fileName;
#endif
            // タブにファイル名表示
            SetButtonName(Path.GetFileName(fileName));
        }

        /// <summary>
        /// 次のウィンドウ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NextWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int nextTabIndex;
            if (GetActiveViewIndex()/*m_activeViewIndex*/ == m_viewInfoMng.Count - 1)
            {
                nextTabIndex = 0;
            }
            else
            {
                nextTabIndex = GetActiveViewIndex()/*m_activeViewIndex*/ + 1;
            }
            ChangeTab(nextTabIndex);
        }

        /// <summary>
        /// 前のウィンドウ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrevWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int prevTabIndex;
            if (GetActiveViewIndex()/*m_activeViewIndex*/ == 0)
            {
                prevTabIndex = m_viewInfoMng.Count - 1;
            }
            else
            {
                prevTabIndex = GetActiveViewIndex()/*m_activeViewIndex*/ - 1;
            }
            ChangeTab(prevTabIndex);
        }

        /// <summary>
        /// 指定行へジャンプ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LineJumpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("指定行ジャンプ 未実装");
        }


        FindDlg fd;

        /// <summary>
        /// フォームのサイズが変更された場合のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void customTextBoxSizeChanged(object sender, EventArgs e)
        {
            if (fd != null)
            {
                Size vScrollbarSize = GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.GetVScrollbarSize(); // 縦スクロールバーサイズ
                Point cp = this.PointToScreen(new Point(this.ClientSize.Width - fd.Width - vScrollbarSize.Width, menuStrip1.Height + tabPanel.Height));
                fd.Location = cp;
            }
        }

        /// <summary>
        /// TextEditorフォームが移動した場合のイベント
        /// 検索フォームが表示させていたら追随させるため
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void customTextBoxMove(object sender, EventArgs e)
        {
            if (fd != null)
            {
                Size vScrollbarSize = GetActiveViewInfo()/*m_activeViewInfo*/.customTextBox.GetVScrollbarSize(); // 縦スクロールバーサイズ
                Point cp = this.PointToScreen(new Point(this.ClientSize.Width - fd.Width - vScrollbarSize.Width, menuStrip1.Height + tabPanel.Height));
                fd.Location = cp;
            }
        }
    }
}
