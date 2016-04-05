using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraTreeList.Data;
using DevExpress.XtraTreeList.Nodes;
using DevExpress.XtraTreeList.Nodes.Operations;
using OpenboxASEditor.EntityModel;

namespace OpenboxASEditor
{
    public partial class Form1 : Form
    {
        SQLiteDatabase _db { get; set; }
        int old_fav_id { get; set; }
        int old_svc_n { get; set; }
        int old_sat_id_number { get; set; }
        string FileName { get; set; }

        public Form1()
        {
            InitializeComponent();

            DB.SetDB(dataSet1);
            repositoryItemLookUpEdit1.DataSource = SvcType;
            SvcTypesLookUpEdit.DataSource = SvcType;
            PolarItemLookUpEdit.DataSource = PolarName;
/*
            SQLite.SQLiteConnection dd = new SQLite.SQLiteConnection(@"c:\WinVSProjects\Openbox AS1 HD\SVC_160326_test.asvc");
            var ex = dd.Table<_svcInfo>().First(w => w.id == 23);
            ex.svc_name = Encoding.GetEncoding("ISO-8859-5").GetBytes(Convert.ToChar(1) + "йцуккенгшщ");

         dd.Update(ex);
           */
        }

        private void barButtonItem2_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FileName = openFileDialog1.FileName;
                var file_name = Path.GetFileName(openFileDialog1.FileName);
                _db = new SQLiteDatabase(openFileDialog1.FileName);

                if (_db.TestConnection())
                {
                    DataTable dt = ExplorerTreeTable;
                    dt.Clear();

                    var project_idx = Guid.NewGuid();
                    dt.Rows.Add(project_idx, Guid.Empty, file_name, -1, "_dbFile", 0);

                    var sat_tree = Guid.NewGuid();
                    dt.Rows.Add(sat_tree, project_idx, "Спутники", -1, "_satTree", 1);

                    var fav_tree = Guid.NewGuid();
                    dt.Rows.Add(fav_tree, project_idx, "Фавориты", -1, "_favTree", 5);

                    var data = DB.GetTreeData(FileName);

                    int sat_id = -1, tp_id = -1, svc_id = -1;
                    Guid sat_parent_id = Guid.Empty;
                    Guid tp_parent_id = Guid.Empty;
                    foreach (DataRow item in data.Rows)
                    {
                        int tp_sat_index = Convert.ToInt32(item["tp_sat_index"]);
                        int svc_tp_index = Convert.ToInt32(item["svc_tp_index"]);
                        int svc_index = Convert.ToInt32(item["id"]);
                        var id = Guid.NewGuid();
                        var row = dt.NewRow();

                        if (sat_id != tp_sat_index)
                        {
                            row["Id"] = id;
                            row["ParentId"] = sat_tree;
                            row["Text"] = item["sat_name"] + ", " + item["sat_angle"] + "°";
                            row["EntityId"] = tp_sat_index;
                            row["EntityType"] = "_satInfo";
                            row["ImageIndex"] = 1;
                            dt.Rows.Add(row);

                            sat_id = tp_sat_index;
                            sat_parent_id = id;
                        }

                        if (tp_id != svc_tp_index)
                        {
                            id = Guid.NewGuid();
                            row = dt.NewRow();
                            row["Id"] = id;
                            row["ParentId"] = sat_parent_id;
                            row["Text"] = item["tp_frequency"] + "," + item["tp_polar_qam"] + "," + item["tp_symbol_rate"] + " [" + item["tp_name"] + "]";
                            row["EntityId"] = svc_tp_index;
                            row["EntityType"] = "_tpInfo";
                            row["ImageIndex"] = 2;

                            dt.Rows.Add(row);
                            tp_id = svc_tp_index;
                            tp_parent_id = id;
                        }

                        if (svc_id != svc_index)
                        {
                            row = dt.NewRow();
                            row["Id"] = Guid.NewGuid();
                            row["ParentId"] = tp_parent_id;
                            row["Text"] = item["svc_name"];
                            row["EntityId"] = svc_index;
                            row["EntityType"] = "_svcInfo";
                            row["ImageIndex"] = Convert.ToInt32(item["svc_type"]) == 0 ? 3 : 4;
                            dt.Rows.Add(row);

                            svc_id = svc_index;
                        }
                    }
                    dt.AcceptChanges();

                    var fav_group = DB.GetFavGroup(-1, _db);

                    barSubItem2.ClearLinks();
                    foreach (DataRow item in fav_group.Rows)
                    {
                        dt.Rows.Add(Guid.NewGuid(), fav_tree, item["fav_name"], item["id"], "_favName", 6);

                        var checkItem = new BarCheckItem(barManager1);
                        checkItem.Caption = item["fav_name"].ToString();

                        checkItem.Id = Convert.ToInt32(item["id"]);
                        checkItem.ItemClick += new ItemClickEventHandler(barCheckFav_ItemClick);
                        barSubItem2.AddItem(checkItem);
                    }
                    FavGroupLookUpEdit.DataSource = fav_group;
                    grpFavGridControl.DataSource = fav_group;

                    ExplorerTree.DataSource = dt;
                    ExplorerTree.ExpandToLevel(1);

                    lnb1GridLookUpEdit.DataSource = DB.GetLNB(_db);
                    lnb_diseqc10LookUpEdit.DataSource = DiSEqC_10Mode;
                    lnb_diseqc11LookUpEdit.DataSource = DiSEqC_11Mode;
                }
            }
        }

        private void treeList1_FocusedNodeChanged(object sender, DevExpress.XtraTreeList.FocusedNodeChangedEventArgs e)
        {
            GetData(e.Node);
        }

        private void gridView3_PopupMenuShowing(object sender, DevExpress.XtraGrid.Views.Grid.PopupMenuShowingEventArgs e)
        {
            if (e.HitInfo.InRow)
            {
                Point p2 = Control.MousePosition;
                this.popupMenu1.ShowPopup(p2);
            }
        }

        private void GetData(TreeListNode node)
        {
            var rv = ExplorerTree.GetDataRecordByNode(node) as DataRowView;
            if (rv != null)
            {
                var r = rv.Row as DataRow;
                if (r["EntityType"].ToString() == "_dbFile")
                {
                    SatGridControl.DataSource = DB.GetSattelites(_db);
                    TpGridControl.DataSource = DB.GetTranspoders(-1, _db);
                    svcGridControl.DataSource = DB.GetSVC(-1, -1, FileName);
                    favGridControl.DataSource = DB.GetFavorites(-1, -1, -1, _db, FileName);

                    TpTabPage.Text = "Транспондеры [" + r["Text"].ToString() + "]";
                    SvcTabPage.Text = "Каналы [" + r["Text"].ToString() + "]";
                    FavTabPage.Text = "Фавориты [" + r["Text"].ToString() + "]";
                }

                if (r["EntityType"].ToString() == "_satInfo" || r["EntityType"].ToString() == "_satTree")
                {
                    int sat_id = Convert.ToInt32(r["EntityId"]);

                    TpGridControl.DataSource = DB.GetTranspoders(sat_id, _db);
                    svcGridControl.DataSource = DB.GetSVC(sat_id, -1, FileName);
                    favGridControl.DataSource = DB.GetFavorites(sat_id, -1, -1, _db, FileName);

                    TpTabPage.Text = "Транспондеры [" + r["Text"].ToString() + "]";
                    SvcTabPage.Text = "Каналы [" + r["Text"].ToString() + "]";
                    FavTabPage.Text = "Фавориты [" + r["Text"].ToString() + "]";
                }

                if (r["EntityType"].ToString() == "_tpInfo")
                {
                    int tp_id = Convert.ToInt32(r["EntityId"]);

                    svcGridControl.DataSource = DB.GetSVC(-1, tp_id, FileName);
                    favGridControl.DataSource = DB.GetFavorites(-1, tp_id, -1, _db, FileName);

                    SvcTabPage.Text = "Каналы [" + r["Text"].ToString() + "]";
                    FavTabPage.Text = "Фавориты [" + r["Text"].ToString() + "]";
                }

                if (r["EntityType"].ToString() == "_favName" || r["EntityType"].ToString() == "_favTree")
                {
                    int fav_id = Convert.ToInt32(r["EntityId"]);
                    favGridControl.DataSource = DB.GetFavorites(-1, -1, fav_id, _db, FileName);
                    FavTabPage.Text = r["Text"].ToString() == "Фавориты" ? r["Text"].ToString() : "Фавориты [" + r["Text"].ToString() + "]";
                }

            }
        }

        private void SatGridView_RowDeleting(object sender, DevExpress.Data.RowDeletingEventArgs e)
        {
            var r = e.Row as DataRowView;
            var sat_id = Convert.ToInt32(r.Row["id"]);
            _db.Delete("_svcInfo", String.Format("svc_tp_index in (select id from _tpInfo where tp_sat_index = {0})", sat_id));
            _db.Delete("_tpInfo", String.Format("tp_sat_index = {0}", sat_id));
            _db.Delete("_satInfo", String.Format("id = {0}", sat_id));

            FindNodeByFieldsValue findNode = new FindNodeByFieldsValue("_satInfo", sat_id);
            ExplorerTree.NodesIterator.DoOperation(findNode);
            ExplorerTree.DeleteNode(findNode.Node);
            ExplorerTreeTable.AcceptChanges();
        }

        private void TpGridView_RowDeleting(object sender, DevExpress.Data.RowDeletingEventArgs e)
        {
            var r = e.Row as DataRowView;
            var tp_id = Convert.ToInt32(r.Row["id"]);
            _db.Delete("_svcInfo", String.Format("svc_tp_index in = {0}", tp_id));
            _db.Delete("_tpInfo", String.Format("id = {0}", tp_id));

            FindNodeByFieldsValue findNode = new FindNodeByFieldsValue("_tpInfo", tp_id);
            ExplorerTree.NodesIterator.DoOperation(findNode);
            ExplorerTree.DeleteNode(findNode.Node);
            ExplorerTreeTable.AcceptChanges();
        }

        private void SVCgridView_RowDeleting(object sender, DevExpress.Data.RowDeletingEventArgs e)
        {
            var r = e.Row as DataRowView;
            var svc_id = Convert.ToInt32(r.Row["id"]);
            _db.Delete("_svcInfo", String.Format("id = {0}", svc_id));

            var sql = @"update _svcInfo set svc_idx = svc_idx - 1 where svc_idx > @svc_lcn and  svc_type = @svc_type";
            var args = new Dictionary<String, Object> { { "svc_type", r["svc_type"] }, { "svc_lcn", r["svc_idx"] } };
            _db.ExecuteNonQuery(sql, args);

            FindNodeByFieldsValue findNode = new FindNodeByFieldsValue("_svcInfo", svc_id);
            ExplorerTree.NodesIterator.DoOperation(findNode);
            ExplorerTree.DeleteNode(findNode.Node);
            ExplorerTreeTable.AcceptChanges();
        }

        private void FavGridView_RowDeleting(object sender, DevExpress.Data.RowDeletingEventArgs e)
        {
            var r = e.Row as DataRowView;
            var fav_id = Convert.ToInt32(r.Row["fav_id"]);
            var svc_id = Convert.ToInt32(r.Row["svc_id"]);
            var _svcInfo = _db.GetDataTable(String.Format("select svc_favorite_index from _svcInfo where id = {0}", svc_id)).Rows.Cast<DataRow>().First();
            if (_svcInfo != null)
            {
                uint favGpNum = (uint)(0x01 << fav_id);
                uint favIdx = Convert.ToUInt16(_svcInfo["svc_favorite_index"]);
                favIdx &= ~favGpNum;

                _db.ExecuteNonQuery(String.Format("update _svcInfo set svc_favorite_index = {0} where id={1}", favIdx, svc_id));
            }
        }

        private void FavGridView_RowUpdated(object sender, DevExpress.XtraGrid.Views.Base.RowObjectEventArgs e)
        {
            var nev_val = e.Row as DataRowView;
            var svc_id = Convert.ToInt32(nev_val.Row["svc_id"]);
            int new_fav_id = Convert.ToInt32(nev_val.Row["fav_id"]);

            if (old_fav_id != new_fav_id)
            {
                DBWorker.GroupFavChange(old_fav_id, new_fav_id, svc_id, _db);
            }
        }

        private void FavGridView_ShowingEditor(object sender, CancelEventArgs e)
        {
           
        }

        private void FavGridView_EditFormShowing(object sender, DevExpress.XtraGrid.Views.Grid.EditFormShowingEventArgs e)
        {
            var id = (FavGridView.GetRow(e.RowHandle) as DataRowView).Row["fav_id"];
            if (id.ToString()!="")
                old_fav_id = Convert.ToInt32(id);
        }

        private void TpGridView_RowUpdated(object sender, DevExpress.XtraGrid.Views.Base.RowObjectEventArgs e)
        {
            var row = (e.Row as DataRowView).Row;
            var sql = @"update _tpInfo set tp_frequency = @tp_frequency,
                               tp_symbol_rate = @tp_symbol_rate,
                               tp_polar_qam=@tp_polar_qam,
                               tp_name=@tp_name
                        where id=@id";
            var args = new Dictionary<String, Object> { 
            { "tp_frequency", row["tp_frequency"] },
            { "tp_symbol_rate", row["tp_symbol_rate"] },
            { "tp_polar_qam", row["tp_polar_qam"] },
            { "tp_name", row["tp_name"] },
            { "id", row["id"] }};

            _db.ExecuteNonQuery(sql, args);
        }

        private void SatGridView_RowUpdated(object sender, DevExpress.XtraGrid.Views.Base.RowObjectEventArgs e)
        {
            var row = (e.Row as DataRowView).Row;
            var sql = @"update _satInfo set sat_name = @sat_name,
                               sat_lnbidx_1st_tuner = @sat_lnbidx_1st_tuner,
                               sat_lnbidx_2nd_tuner=@sat_lnbidx_2nd_tuner,
                               sat_id_number = @sat_id_number 
                        where id=@id";
            var args = new Dictionary<String, Object> { 
            { "sat_name", row["sat_name"] },
            { "sat_lnbidx_1st_tuner", row["sat_lnbidx_1st_tuner"] },
            { "sat_lnbidx_2nd_tuner", row["sat_lnbidx_2nd_tuner"] },
            { "sat_id_number", row["sat_id_number"] },
            { "id", row["id"] }};

            _db.ExecuteNonQuery(sql, args);

            int new_sat_id_number = Convert.ToInt32(row["sat_id_number"]);
            int id = Convert.ToInt32(row["id"]);
            args = new Dictionary<String, Object> { { "new_sat_id_number", new_sat_id_number }, { "old_sat_id_number", old_sat_id_number }, { "id", id } };

            if (old_sat_id_number > new_sat_id_number) 
             {
                 sql = @"update _satInfo set sat_id_number = sat_id_number + 1 where sat_id_number >= @new_sat_id_number and  sat_id_number < @old_sat_id_number and id <> @id";
                 _db.ExecuteNonQuery(sql, args);
             }
            if (old_sat_id_number < new_sat_id_number)
            {
                sql = @"update _satInfo set sat_id_number = sat_id_number - 1 where sat_id_number > @old_sat_id_number and sat_id_number <= @new_sat_id_number and id <> @id";
                _db.ExecuteNonQuery(sql, args);
            }
        }

        private void SVCgridView_RowUpdated(object sender, DevExpress.XtraGrid.Views.Base.RowObjectEventArgs e)
        {
            var row = (e.Row as DataRowView).Row;
            int new_svc_lcn = Convert.ToInt32(row["svc_idx"]) - 1;

            var sql = @"update _svcInfo set svc_name = @svc_name,
                               svc_pmt_pid = @svc_pmt_pid,
                               svc_id=@svc_id,
                               svc_pcr_pid=@svc_pcr_pid,
                               svc_video_pid=@svc_video_pid,
                               svc_audio_pid=@svc_audio_pid,
                               svc_is_lock=@svc_is_lock,
                               svc_idx=@svc_idx
                        where id=@id";

            var args = new Dictionary<String, Object> { 
            { "svc_name", DB.StringToISO(Convert.ToChar(row["code_page"]) + row["svc_name"].ToString()) /*Encoding.GetEncoding("ISO-8859-5").GetBytes(Convert.ToChar(row["code_page"]) + row["svc_name"].ToString())*/ },
            { "svc_pmt_pid", row["svc_pmt_pid"] },
            { "svc_id", row["svc_id"] },
            { "svc_pcr_pid", row["svc_pcr_pid"] },
            { "svc_video_pid", row["svc_video_pid"] },
            { "svc_audio_pid", row["svc_audio_pid"] },
            { "svc_idx", new_svc_lcn },
            { "svc_is_lock", row["svc_is_lock"] },
            { "id", row["id"] }};

            _db.ExecuteNonQuery(sql, args);
           
            int svc_type = Convert.ToInt32(row["svc_type"]);
            int id = Convert.ToInt32(row["id"]);

            if (old_svc_n > new_svc_lcn) 
             {
                 sql = @"update _svcInfo set svc_idx = svc_idx + 1 where svc_idx >= @new_svc_lcn and  svc_idx < @old_svc_n and svc_type = @svc_type and id <> @id";
                 args = new Dictionary<String, Object> { { "svc_type", svc_type }, { "new_svc_lcn", new_svc_lcn }, { "old_svc_n", old_svc_n }, { "id", id } };
                 _db.ExecuteNonQuery(sql, args);
             }
            if (old_svc_n < new_svc_lcn)
            {
                sql = @"update _svcInfo set svc_idx = svc_idx - 1 where svc_idx > @old_svc_n and svc_idx <= @new_svc_lcn and svc_type = @svc_type and id <> @id";
                args = new Dictionary<String, Object> { { "svc_type", svc_type }, { "new_svc_lcn", new_svc_lcn }, { "old_svc_n", old_svc_n }, { "id", id } };
                _db.ExecuteNonQuery(sql, args);
            }
        }

        private void barSubItem2_Popup(object sender, EventArgs e)
        {
            var r = SVCgridView.GetDataRow(SVCgridView.FocusedRowHandle);
            uint svc_favorite_index = Convert.ToUInt32(r["svc_favorite_index"]);
            var sub = sender as BarSubItem;
            foreach (BarItemLink item in sub.ItemLinks)
            {
                var ch = item.Item as BarCheckItem;
                if (SVCgridView.SelectedRowsCount == 1)
                {
                    ch.Checked = DB.CheckFav(ch.Id, svc_favorite_index);
                }
                else
                {
                    ch.Checked = false;
                }
            }
        }

        private void barCheckFav_ItemClick(object sender, ItemClickEventArgs e)
        {
            var ch = e.Item as BarCheckItem;

            foreach (int handle in SVCgridView.GetSelectedRows())
            {
                var r = SVCgridView.GetDataRow(handle);
                int svc_id = Convert.ToInt32(r["id"]);
                if (ch.Checked)
                {
                    r["svc_favorite_index"] = DBWorker.SetFav(ch.Id, svc_id, _db);
                }
                else
                {
                    r["svc_favorite_index"] = DBWorker.DelFav(ch.Id, svc_id, _db);
                }
            }
        }

        private void SVCgridView_EditFormShowing(object sender, DevExpress.XtraGrid.Views.Grid.EditFormShowingEventArgs e)
        {
            old_svc_n = Convert.ToInt32((SVCgridView.GetRow(e.RowHandle) as DataRowView).Row["svc_idx"]) - 1;
        }

        private void GrpFavGridView_RowUpdated(object sender, DevExpress.XtraGrid.Views.Base.RowObjectEventArgs e)
        {
            var row = (e.Row as DataRowView).Row;
            var sql = @"update _favName set fav_name = @fav_name where id=@id";
            var args = new Dictionary<String, Object> { { "fav_name", row["fav_name"] }, { "id", row["id"] } };

            _db.ExecuteNonQuery(sql, args);
        }

        private void delSVCBtn_ItemClick(object sender, ItemClickEventArgs e)
        {
            SVCgridView.DeleteSelectedRows();
        }

        private void barButtonItem5_ItemClick(object sender, ItemClickEventArgs e)
        {
            SVCgridView.ShowEditForm();
        }

        private void SatGridView_EditFormShowing(object sender, DevExpress.XtraGrid.Views.Grid.EditFormShowingEventArgs e)
        {
            old_sat_id_number = Convert.ToInt32((SatGridView.GetRow(e.RowHandle) as DataRowView).Row["sat_id_number"]);
        }

        private void barButtonItem6_ItemClick(object sender, ItemClickEventArgs e)
        {
            Process.Start(@"http://www.openboxfan.com/");
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Process.Start(@"http://www.openboxfan.com/");
        }

        private void barButtonItem7_ItemClick(object sender, ItemClickEventArgs e)
        {
            var point = SVCgridView.GetDataRow(SVCgridView.FocusedRowHandle) as DataRow;
            int new_svc_idx = Convert.ToInt32(point["svc_idx"]) - 1;

            int count = 0;
            foreach (int item in SVCgridView.GetSelectedRows())
            {
                var select_row = SVCgridView.GetDataRow(item) as DataRow;

                int old_svc_idx = Convert.ToInt32(select_row["svc_idx"]) - 1;
                int id = Convert.ToInt32(select_row["id"]);
                int svc_type = Convert.ToInt32(select_row["svc_type"]);

                var sql = @"update _svcInfo set svc_idx=@svc_idx where id=@id";

                var args = new Dictionary<String, Object> { 
                      { "svc_idx", new_svc_idx },
                      { "id", id }};

                _db.ExecuteNonQuery(sql, args);

                if (old_svc_idx > new_svc_idx)
                {
                    sql = @"update _svcInfo set svc_idx = svc_idx + 1 where svc_idx >= @new_svc_lcn and  svc_idx < @old_svc_n and svc_type = @svc_type and id <> @id";
                    args = new Dictionary<String, Object> { { "svc_type", svc_type }, { "new_svc_lcn", new_svc_idx }, { "old_svc_n", old_svc_idx }, { "id", id } };
                    _db.ExecuteNonQuery(sql, args);

                    new_svc_idx += 1;
                }
                if (old_svc_idx < new_svc_idx)
                {
                    sql = @"update _svcInfo set svc_idx = svc_idx - 1 where svc_idx > @old_svc_n and svc_idx <= @new_svc_lcn and svc_type = @svc_type and id <> @id";
                    args = new Dictionary<String, Object> { { "svc_type", svc_type }, { "new_svc_lcn", new_svc_idx }, { "old_svc_n", old_svc_idx - (++count) }, { "id", id } };
                    _db.ExecuteNonQuery(sql, args);
                }

               
            }

            GetData(ExplorerTree.FocusedNode);
        }

        private void barButtonItem8_ItemClick(object sender, ItemClickEventArgs e)
        {
            ExplorerTree.FocusedNode = ExplorerTree.Nodes[0];

            GetData(ExplorerTree.Nodes[0]);

            int tv = 0, radio = 0;
            StringBuilder sb = new StringBuilder();
            SQLiteConnection cnn = new SQLiteConnection(String.Format("Data Source={0}", openFileDialog1.FileName));
            cnn.Open();
            SQLiteTransaction trans =  cnn.BeginTransaction();
            
            
 
 
            for (int h = 0; h < SVCgridView.RowCount; ++h)
            {
                var r = SVCgridView.GetDataRow(h) as DataRow;
                int svc_type = Convert.ToInt32(r["svc_type"]);
                int id = Convert.ToInt32(r["id"]);

          /*      if (svc_type == 0)
                {
                    sb.AppendLine(String.Format("update _svcInfo set svc_idx={0} where id={1} ;", tv, id));
                    ++tv;
                }
                if (svc_type == 1)
                {
                    sb.AppendLine(String.Format("update _svcInfo set svc_idx={0} where id={1} ;", radio, id));
                    ++radio;
                }*/

                var sql = @"update _svcInfo set svc_idx=@svc_idx where id=@id";

                var args = new Dictionary<String, Object> { { "id", id } };
                if (svc_type == 0)
                {
                    args.Add("svc_idx", tv);
                    ++tv;
                }
                if (svc_type == 1)
                {
                    args.Add("svc_idx", radio);
                    ++radio;
                }

                SQLiteCommand mycommand = new SQLiteCommand(cnn);
                foreach (var key_value_pair in args)
                {
                    mycommand.Parameters.AddWithValue(key_value_pair.Key, key_value_pair.Value);
                }

                mycommand.CommandText = sql;
                int rowsUpdated = mycommand.ExecuteNonQuery();  

            //   _db.ExecuteNonQuery(sql, args);

                barEditItem1.EditValue = (h * 100) / SVCgridView.RowCount;
                barEditItem1.Refresh();
            }
            trans.Commit();

            cnn.Close();  
         //   _db.ExecuteNonQuery(sb.ToString());

            GetData(ExplorerTree.FocusedNode);
            barEditItem1.EditValue = 0;
            barEditItem1.Refresh();
        }

        private void popupMenu1_Popup(object sender, EventArgs e)
        {
            MoveSVCBtn.Enabled = (!SVCgridView.IsRowSelected(SVCgridView.FocusedRowHandle) && SVCgridView.SelectedRowsCount > 0);
        }
    }

    public class FindNodeByFieldsValue : TreeListOperation
    {
        public const string EntityTypeColumnName = "EntityType";
        public const string EntityIdColumnName = "EntityId";
        private TreeListNode nodeCore;
        private object EntityTypeCore;
        private object EntityIdCore;
        private bool isNullCore;
        public FindNodeByFieldsValue(object EntityType, object EntityId)
        {
            this.EntityTypeCore = EntityType;
            this.EntityIdCore = EntityId;
            this.nodeCore = null;
            this.isNullCore = TreeListData.IsNull(EntityTypeCore) || TreeListData.IsNull(EntityIdCore);
        }
        public override void Execute(TreeListNode node)
        {
            if (IsLookedFor(node.GetValue(EntityTypeColumnName), node.GetValue(EntityIdColumnName)))
                this.nodeCore = node;
        }
        bool IsLookedFor(object EntityType, object EntityId)
        {
            if (IsNull) return (EntityTypeCore == EntityType && EntityIdCore == EntityId);
            return EntityTypeCore.Equals(EntityType) && EntityIdCore.Equals(EntityId);
        }
        protected bool IsNull { get { return isNullCore; } }
        public override bool CanContinueIteration(TreeListNode node) { return Node == null; }
        public TreeListNode Node { get { return nodeCore; } }
    }
}
