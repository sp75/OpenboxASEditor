using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenboxASEditor.EntityModel;

namespace OpenboxASEditor
{
    public static class DB
    {
        public static void SetDB(DataSet _dataset)
        {
            var PolarName = _dataset.Tables["PolarName"];
            var SvcType = _dataset.Tables["SvcType"];
            var DiSEqC_10Mode = _dataset.Tables["DiSEqC_10Mode"];
            var DiSEqC_11Mode = _dataset.Tables["DiSEqC_11Mode"];
            var TypLNBMode = _dataset.Tables["TypLNBMode"];

            PolarName.Rows.Add(0, "V");
            PolarName.Rows.Add(1, "H");

            SvcType.Rows.Add(0, "TV");
            SvcType.Rows.Add(1, "Radio");

            DiSEqC_10Mode.Rows.Add(0, "Off");
            DiSEqC_10Mode.Rows.Add(1, "1/4");
            DiSEqC_10Mode.Rows.Add(2, "2/4");
            DiSEqC_10Mode.Rows.Add(3, "3/4");
            DiSEqC_10Mode.Rows.Add(4, "4/4");

            DiSEqC_11Mode.Rows.Add(0, "Off");
            DiSEqC_11Mode.Rows.Add(1, "1/16");
            DiSEqC_11Mode.Rows.Add(2, "2/16");
            DiSEqC_11Mode.Rows.Add(3, "3/16");
            DiSEqC_11Mode.Rows.Add(4, "4/16");
            DiSEqC_11Mode.Rows.Add(5, "5/16");
            DiSEqC_11Mode.Rows.Add(6, "6/16");
            DiSEqC_11Mode.Rows.Add(7, "7/16");
            DiSEqC_11Mode.Rows.Add(8, "8/16");
            DiSEqC_11Mode.Rows.Add(9, "9/16");
            DiSEqC_11Mode.Rows.Add(10, "10/16");
            DiSEqC_11Mode.Rows.Add(11, "11/16");
            DiSEqC_11Mode.Rows.Add(12, "12/16");
            DiSEqC_11Mode.Rows.Add(13, "13/16");
            DiSEqC_11Mode.Rows.Add(14, "14/16");
            DiSEqC_11Mode.Rows.Add(15, "15/16");
            DiSEqC_11Mode.Rows.Add(16, "16/16");

            TypLNBMode.Rows.Add(0, "Single");
            TypLNBMode.Rows.Add(1, "Universal");
            TypLNBMode.Rows.Add(2, "OCS-DP");
            TypLNBMode.Rows.Add(3, "Legacy Twin 1");
            TypLNBMode.Rows.Add(4, "Legacy Twin 2");
            TypLNBMode.Rows.Add(5, "Legacy Quad 1");
            TypLNBMode.Rows.Add(6, "Legacy Quad 2");

            _dataset.AcceptChanges();
        }

        public static bool CheckFav(int NumFav, uint FavIdx)
        {
            uint favGpNum = (uint)(0x01 << NumFav);
            return Convert.ToBoolean(favGpNum & FavIdx);
        }

        public static DataTable GetSattelites(SQLiteDatabase db)
        {
            return db.GetDataTable(@"SELECT _satInfo.id, sat_id_number,sat_name, 
                                   (case when (sat_angle / 10.00) > 180 then round(360.00 - (sat_angle / 10.00), 2) else  round((sat_angle / 10.00),2) end  ) sat_angle ,
                                   (case when (sat_angle / 10.00) > 180 then 'W' else 'E' end  ) l ,
                                   lnb1.lnb_used as lnb_used_1st_tuner,
                                   lnb2.lnb_used as lnb_used_2nd_tuner,
                                   sat_lnbidx_1st_tuner,
                                   sat_lnbidx_2nd_tuner

                                   FROM _satInfo  
                                   left outer join _lnbInfo lnb1 on _satInfo.sat_lnbidx_1st_tuner = lnb1.id
                                   left outer join _lnbInfo lnb2 on _satInfo.sat_lnbidx_2nd_tuner = lnb2.id
                                   order by sat_id_number ");
        }

        public static DataTable GetLNB(SQLiteDatabase db)
        {
            return db.GetDataTable(@"select * from _lnbInfo");
        }

        public static DataTable GetTranspoders(int sat_index, SQLiteDatabase db)
        {
            return db.GetDataTable(String.Format(@"SELECT _tpInfo.id
                                    ,tp_frequency
                                    ,tp_symbol_rate
                                    ,tp_polar_qam
                                    ,tp_used
                                    ,tp_name
                                    ,tp_sat_index
                                    ,_satInfo.sat_name
                                    FROM _tpInfo
                                    inner join _satInfo on tp_sat_index = _satInfo.id
                                    where tp_sat_index = {0} or -1 = {0}", sat_index));
        }

        public static DataTable GetSVC(int sat_index, int tp_index, string file_name)
        {
            string q = String.Format(@"SELECT _svcInfo.id,
            (svc_idx + 1) svc_idx ,
            svc_type,
            svc_tp_index, 
            svc_name,
            svc_id,
            svc_lcn,
            svc_pmt_pid,
            svc_pcr_pid,
            svc_video_pid,
            svc_audio_pid,
            svc_is_lock,
            tp_sat_index, 
            ( cast (tp_frequency as varchar(50)) || ',' || ( CASE WHEN [tp_polar_qam] = 1 THEN 'H' ELSE 'V' END ) || ',' || cast (tp_symbol_rate as varchar(50)) || ' [' || tp_name || ']' ) tp_name,
            sat_id_number,
            sat_name,
            (sat_angle /10.00) as sat_angle,
            svc_favorite_index
            FROM _svcInfo
            inner join _tpInfo on svc_tp_index = _tpInfo.id
            inner join _satInfo on tp_sat_index = _satInfo.id
            where (_satInfo.id = {0} or -1 = {0}) and (_tpInfo.id = {1} or -1 = {1})
            order by svc_type,  svc_idx", sat_index, tp_index);

            SQLite.SQLiteConnection dd = new SQLite.SQLiteConnection(file_name);

            var query = dd.Query<SvcList>(q);
            return query.Select(s => new
            {
                s.id,
                s.svc_idx,
                s.svc_type,
                s.svc_tp_index,
                code_page = s.svc_name[0] < 21 ? s.svc_name[0] : 0,
                svc_name = ISOToString(s.svc_name[0] < 21 ? s.svc_name.Skip(1).ToArray() : s.svc_name),
                s.svc_id,
                s.svc_lcn,
                s.svc_pmt_pid,
                s.svc_pcr_pid,
                s.svc_video_pid,
                s.svc_audio_pid,
                s.svc_is_lock,
                s.tp_sat_index,
                s.tp_name,
                s.sat_id_number,
                s.sat_name,
                s.sat_angle,
                s.svc_favorite_index
            }).ToDataTable();
        }


        public static DataTable GetFavGroup(int fav_index, SQLiteDatabase db)
        {
            var fav = db.GetDataTable(String.Format(@"SELECT id, fav_name FROM _favName where id = {0} or -1 = {0}", fav_index));
            foreach (DataRow r in fav.Rows)
            {
                r["fav_name"] = FavNameDef(r["fav_name"].ToString());
            }
            fav.AcceptChanges();

            return fav;
        }

        public static DataTable GetFavorites(int sat_index, int tp_index, int fav_index, SQLiteDatabase db, string file_name)
        {
            var fav_name = GetFavGroup(fav_index, db);
            var Favorites = db.GetDataTable(@"SELECT 0 id, 
                                                    '' fav_name, 
                                                    '' svc_name,
                                                     0 fav_id, 
                                                     0 svc_id,
                                                     0 svc_type,
                                                    '' tp_name,
                                                    '' sat_name
                                              ");
            Favorites.Clear();

            var svc_data_table = GetSVC(sat_index, tp_index, file_name);
            int fav_count = 0;
            foreach (DataRow svc_item in svc_data_table.Rows)
            {
                uint fav_idx = Convert.ToUInt32(svc_item["svc_favorite_index"]);
                if (fav_idx == 0)
                {
                    continue;
                }

                foreach (DataRow fav_item in fav_name.Rows)
                {
                    int fav_id = Convert.ToInt32(fav_item["id"]);
                    if (DB.CheckFav(fav_id, fav_idx))
                    {
                        var f_row = Favorites.NewRow();

                        f_row["id"] = ++fav_count;
                        f_row["fav_name"] = fav_item["fav_name"];
                        f_row["svc_name"] = svc_item["svc_name"];
                        f_row["fav_id"] = fav_id;
                        f_row["svc_id"] = svc_item["id"];
                        f_row["svc_type"] = svc_item["svc_type"];
                        f_row["tp_name"] = svc_item["tp_name"];
                        f_row["sat_name"] = svc_item["sat_name"];

                        Favorites.Rows.Add(f_row);
                    }
                }
            }
            Favorites.AcceptChanges();

            return Favorites;
        }

        public static DataTable GetTreeData(string file_name)
        {
            string q = @"SELECT _svcInfo.id,
                                     svc_idx,
                                     svc_type,
                                     svc_tp_index, 
                                     svc_name,
                                     tp_sat_index, 
                                     tp_frequency, 
                                     tp_symbol_rate, 
                                     ( CASE WHEN [tp_polar_qam] = 1 THEN 'H' ELSE 'V' END ) tp_polar_qam, 
                                     tp_name,
                                     sat_id_number,
                                     sat_name,
                                     (sat_angle /10.00) as sat_angle
                                     FROM _svcInfo
                                     inner join _tpInfo on svc_tp_index = _tpInfo.id
                                     inner join _satInfo on tp_sat_index = _satInfo.id
                                     order by sat_id_number, tp_frequency, svc_type,  svc_lcn";

            SQLite.SQLiteConnection dd = new SQLite.SQLiteConnection(file_name);

            var query = dd.Query<TreeList>(q);
            return query.Select(s => new
            {
                s.id,
                s.svc_idx,
                s.svc_type,
                s.svc_tp_index,
                svc_name = ISOToString(s.svc_name[0] < 21 ? s.svc_name.Skip(1).ToArray() : s.svc_name),
                s.tp_sat_index,
                s.tp_frequency,
                s.tp_symbol_rate,
                s.tp_polar_qam,
                s.tp_name,
                s.sat_id_number,
                s.sat_name,
                s.sat_angle
            }).ToDataTable();
        }

        public static String ISOToString(byte[] name)
        {
            return Encoding.GetEncoding("ISO-8859-5").GetString(name);
        }

        public static byte[] StringToISO(String str)
        {
            return Encoding.GetEncoding("ISO-8859-5").GetBytes(str);
        }

        public static String FavNameDef(string tag)
        {
            switch (tag)
            {
                case "<&>favg_sports": return "Спорт";
                case "<&>favg_movie": return "Кино";
                case "<&>favg_drama": return "Драма";
                case "<&>favg_news": return "Новости";
                case "<&>favg_music": return "Музыка";
                case "<&>favg_ani": return "Мультфильмы";
                case "<&>favg_edu": return "Образование";
                case "<&>favg_doc": return "Документальный";
                case "<&>favg_cook": return "Кулинария";
                case "<&>favg_shop": return "Покупки";
                case "<&>favg_travel": return "Путешествия";
                case "<&>favg_adult": return "Для взрослых";
                case "<&>hd": return "HD";
            }
            return tag;
        }

    }
}

       