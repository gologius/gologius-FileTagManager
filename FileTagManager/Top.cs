﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs; //add 

namespace FileTagManager
{
    public partial class Top : Form
    {
        private string currentPath = "";
        private TagList tagList;
        private ImagePreviewForm imgform;
        private int prevSelectRow = -1; //前回選択していたセル行番号

        public Top()
        {
            InitializeComponent();

            tagList = TagList.getTagList(Config.TAGFILE_PATH); //タグ設定情報を読み込み
            
            //ファイルのD&Dにて起動されたなら（＝コマンドライン引数で、ファイルパスが設定されているなら）
            string[] cmds = System.Environment.GetCommandLineArgs();
            if (cmds.Count() == 2)
            {
                //ファイル名が紛れ込んでいる場合、それを削除する
                string dirPath = System.IO.Path.GetDirectoryName(cmds[1]);

                this.Text = "File Tag Manager - " + dirPath;
                currentPath = dirPath;
                decideChangeNameButton.Enabled = true; //ボタンを有効にする
                overrideButton.Enabled = true;

                updateFileNameView(dirPath);
            }
        }

        /// <summary>
        /// fileNameViewから値を得るためのラッパー
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        private string viewValue(int rowIndex, int colIndex)
        {
            return fileNameView.Rows[rowIndex].Cells[colIndex].Value.ToString();
        }

        /// <summary>
        /// 指定されたフォルダのパスから，ファイルの文字列を表示する．
        /// </summary>
        /// <param name="selectPath">ファイルを読み込むフォルダのパス</param>
        private void updateFileNameView(string selectPath)
        {
            if (currentPath.Equals(""))
                return;

            //列がない(=初回)場合は列を追加する
            //元のファイル名表示(1列)+タグ数 
            if (fileNameView.Columns.Count < Config.MAX_TAG_NUM + 1)
            {
                for (int i = 0; i < Config.MAX_TAG_NUM + 1; i++)
                {
                    fileNameView.Columns.Add(new DataGridViewTextBoxColumn());
                }

                //元のファイル名を表示する列
                fileNameView.Columns[0].DataPropertyName = "FileName";
                fileNameView.Columns[0].Name = "FileName";
                fileNameView.Columns[0].HeaderText = "FileName";
                fileNameView.Columns[0].ReadOnly = true;
            }

            updateHeaderText(); //ヘッダーの更新
            fileNameView.Rows.Clear(); //表を初期化

            //指定パスにあるファイルを取得して，行を追加していく(列は0番目)．
            var fullpaths = System.IO.Directory.GetFiles(selectPath, "*");
            foreach (var f in fullpaths)
            {
                fileNameView.Rows.Add(Path.GetFileName(f)); //行をset
            }

            extractString(); //tagの設定に合わせて文字列を抽出，反映

            //左端の列の色を変更
            for (int i = 0; i < fileNameView.Rows.Count; i++)
            {
                fileNameView[0, i].Style.BackColor = Color.LightBlue;
            }
        }

        /// <summary>
        /// Viewの列ヘッダーテキストを更新する(表の最上部の行のテキスト)
        /// </summary>
        public void updateHeaderText()
        {
            if (fileNameView.Columns.Count < Config.MAX_TAG_NUM + 1)
                return; //列が足りない(まだ追加されていない)

            //タグ表示列
            for (int i = 0; i < Config.MAX_TAG_NUM; i++)
            {
                //+1は元のファイル名表示列の分
                string tagname = tagList.tags[i].name;
                fileNameView.Columns[i + 1].DataPropertyName = tagname;
                fileNameView.Columns[i + 1].Name = tagname;
                fileNameView.Columns[i + 1].HeaderText = tagname;
            }
        }

        /// <summary>
        /// 抽出設定を基に，文字列を抽出する．
        /// </summary>
        private void extractString()
        {
            //全ファイル名に対してTag数分の文字列処理をする
            for (int i = 0; i < fileNameView.Rows.Count; i++) //行
            {
                for (int j = 0; j < Config.MAX_TAG_NUM; j++) //タグ(列)
                {
                    string target = fileNameView.Rows[i].Cells[0].Value.ToString(); //ファイル名を取得
                    fileNameView.Rows[i].Cells[j + 1].Value = tagList.tags[j].replace(target); //セルに挿入
                }
            }
        }

        /// <summary>
        /// 画像ビューワーの表示
        /// </summary>
        /// <param name="isShow">trueなら表示，falseなら非表示</param>
        private void showViewer(bool isShow)
        {
            //表示
            if (isShow)
            {
                //ビューワーを複数起動を許さないため、フォームが閉じられているか確認する
                if (imgform != null)
                {
                    if (imgform.IsDisposed == false)
                    {
                        return;
                    }
                }

                imgform = new ImagePreviewForm();
                imgform.Show();

                //選択されているセル行のZIPファイル名を取得
                if (fileNameView.SelectedCells.Count > 0)
                {
                    int select_row = fileNameView.SelectedCells[0].RowIndex; //選択しているセルの行番号取得
                    imgform.setZip(@currentPath + @"\" + viewValue(select_row, 0));
                }
            }
            //非表示
            else
            {
                if (imgform != null)
                {
                    //フォームを閉じる
                    imgform.clear(); //file close
                    imgform.Dispose();
                }
            }
        }

        //##############################################################################################################

        /// <summary>
        /// 編集対象のフォルダを指定するウインドウを開く．
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DirOpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //フォルダを開くダイアログの準備
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;  // フォルダーを開く設定に
            dialog.EnsureReadOnly = false;
            dialog.AllowNonFileSystemItems = false;
            dialog.DefaultDirectory = Application.StartupPath;

            //フォルダを開く
            var Result = dialog.ShowDialog();
            if (Result == CommonFileDialogResult.Ok)
            {
                this.Text = "File Tag Manager - " + dialog.FileName;
                currentPath = dialog.FileName;
                decideChangeNameButton.Enabled = true; //ボタンを有効にする
                overrideButton.Enabled = true;

                updateFileNameView(dialog.FileName);
            }
        }


        /// <summary>
        /// タグを編集するウインドウを開く．
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StringConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //タグ設定フォームを生成
            TagConfig form = new TagConfig();
            form.ShowDialog(this); //編集終了までTOPには戻らない
            form.Dispose();

            updateHeaderText(); //タグ名が変わっている場合があるため，更新する
            updateFileNameView(currentPath); //抽出条件が変わる場合があるので更新する．
        }

        /// <summary>
        /// フォーマット文字列を基に，ファイル名を変更する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void decideChangeNameButton_Click(object sender, EventArgs e)
        {
            //確認ダイアログ
            DialogResult decide = MessageBox.Show(
                "ファイルの名前を一括で変更しますか？",
                "確認",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.None,
                MessageBoxDefaultButton.Button2);
            if (decide == DialogResult.Cancel)
            {
                return;
            }

            //ビューワーを一回落とす(ファイルアクセス中に名前を変更できない)
            showViewer(false);

            //選択されているセルから，行番号を重複がないように抽出する
            List<int> indices = new List<int>();
            foreach (DataGridViewCell c in fileNameView.SelectedCells)
            {
                indices.Add(c.RowIndex);
            }
            indices = indices.Distinct().ToList<int>(); //重複の削除

            //選択された行だけ対象に，文字を置換していく
            foreach (int select_index in indices)
            {
                string result = formatText.Text; //テンプレート文字列を取り出す

                //%で囲んだタグ名を，文字列に置き換える
                foreach (Tag tag in tagList.tags)
                {
                    string cell_string = viewValue(select_index, tagList.getTagIndex(tag.name) + 1); //+1は元のファイル名の分
                    result = result.Replace("%" + tag.name + "%", cell_string);
                }

                try
                {
                    //実際にファイル名を変更する
                    System.IO.File.Move(
                        @currentPath + @"\" + viewValue(select_index, 0),
                        @currentPath + @"\" + result);

                }
                catch (IOException ex)
                {
                    MessageBox.Show(
                    "\"" + viewValue(select_index, 0) + "\" > \"" + result + "\"\n" + ex.Message,
                    "ファイル名変更時エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button2);
                }
            }
            //確認ダイアログ
            MessageBox.Show(
                "変更が終了しました",
                "確認",
                MessageBoxButtons.OK,
                MessageBoxIcon.None,
                MessageBoxDefaultButton.Button2);

            //ファイルのパスが変わるので，表示を更新する
            updateFileNameView(currentPath);
        }

        /// <summary>
        /// Zipファイルにアクセスして，別のウインドウにZIPファイル内の画像を表示する．
        /// Viewでセル選択が別のセルに移ったときに場合に呼ばれる．
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileNameView_SelectionChanged(object sender, EventArgs e)
        {
            //フォームが生成されていない or 閉じられている場合はこの処理をしない
            if (imgform == null)
                return;
            if (imgform.IsDisposed)
                return;

            //ZIPファイルをViewerに設定
            if (fileNameView.SelectedCells.Count > 0)
            {
                int select_row = fileNameView.SelectedCells[0].RowIndex; //選択しているセルの行番号取得
                //前回選択していた行と違う行なら
                if (select_row != prevSelectRow)
                {
                    imgform.setZip(@currentPath + @"\" + viewValue(select_row, 0));
                    prevSelectRow = select_row; //更新
                }
            }
        }

        /// <summary>
        /// 選択されたセル内のタグを上書き設定する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void overrideButton_Click(object sender, EventArgs e)
        {
            //確認ダイアログ
            DialogResult decide = MessageBox.Show(
                "タグを一括で設定しますか？",
                "確認",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.None,
                MessageBoxDefaultButton.Button2);
            if (decide == DialogResult.Cancel)
            {
                return;
            }

            //選択されているセルを抽出
            string override_text = overrideTextBox.Text;
            foreach (DataGridViewCell cell in fileNameView.SelectedCells)
            {
                //textboxで設定されている文字列に変更する
                fileNameView.Rows[cell.RowIndex].Cells[cell.ColumnIndex].Value = override_text;
            }

        }

        /// <summary>
        /// プレビューボタンを押した際の挙動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void previewButton_Click(object sender, EventArgs e)
        {
            showViewer(true);
        }
    }
}
