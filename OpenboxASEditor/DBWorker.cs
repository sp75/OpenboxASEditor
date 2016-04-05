using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenboxASEditor
{
    public static class DBWorker
    {
        public static void GroupFavChange(int old_fav_id, int new_fav_id, int svc_id,  SQLiteDatabase db)
        {
            var _svcInfo = db.GetDataTable(String.Format("select svc_favorite_index from _svcInfo where id = {0}", svc_id)).Rows.Cast<DataRow>().First();
            if (_svcInfo != null)
            {
                uint favIdx = Convert.ToUInt16(_svcInfo["svc_favorite_index"]);

                uint favGpNum = (uint)(0x01 << old_fav_id);
                favIdx &= ~favGpNum;

                favGpNum = (uint)(0x01 << new_fav_id);
                favIdx |= favGpNum;
                
                db.ExecuteNonQuery(String.Format("update _svcInfo set svc_favorite_index = {0} where id={1}", favIdx, svc_id));
            }
        }

        public static uint SetFav(int fav_id, int svc_id, SQLiteDatabase db)
        {
            uint favIdx = 0;
            var _svcInfo = db.GetDataTable(String.Format("select svc_favorite_index from _svcInfo where id = {0}", svc_id)).Rows.Cast<DataRow>().First();
            if (_svcInfo != null)
            {
                favIdx = Convert.ToUInt16(_svcInfo["svc_favorite_index"]);

                uint favGpNum = (uint)(0x01 << fav_id);
                favIdx |= favGpNum;

                db.ExecuteNonQuery(String.Format("update _svcInfo set svc_favorite_index = {0} where id={1}", favIdx, svc_id));
            }
            return favIdx;
        }

        public static uint DelFav(int fav_id, int svc_id, SQLiteDatabase db)
        {
            uint favIdx = 0;
            var _svcInfo = db.GetDataTable(String.Format("select svc_favorite_index from _svcInfo where id = {0}", svc_id)).Rows.Cast<DataRow>().First();
            if (_svcInfo != null)
            {
                favIdx = Convert.ToUInt16(_svcInfo["svc_favorite_index"]);

                uint favGpNum = (uint)(0x01 << fav_id);
                favIdx &= ~favGpNum;

                db.ExecuteNonQuery(String.Format("update _svcInfo set svc_favorite_index = {0} where id={1}", favIdx, svc_id));
            }

            return favIdx;
        }


        public static DataTable ToDataTable<T>(this IEnumerable<T> varlist)
        {
            DataTable dtReturn = new DataTable();

            // column names 
            PropertyInfo[] oProps = null;
            FieldInfo[] oField = null;
            if (varlist == null) return dtReturn;

            foreach (T rec in varlist)
            {
                // Use reflection to get property names, to create table, Only first time, others will follow 
                if (oProps == null)
                {
                    oProps = rec.GetType().GetProperties();
                    foreach (PropertyInfo pi in oProps)
                    {
                        Type colType = pi.PropertyType;

                        if ((colType.IsGenericType) &&
                             (colType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                        {
                            colType = colType.GetGenericArguments()[0];
                        }

                        dtReturn.Columns.Add(new DataColumn(pi.Name, colType));
                    }
                    oField = rec.GetType().GetFields();
                    foreach (FieldInfo fieldInfo in oField)
                    {
                        Type colType = fieldInfo.FieldType;

                        if ((colType.IsGenericType) &&
                             (colType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                        {
                            colType = colType.GetGenericArguments()[0];
                        }

                        dtReturn.Columns.Add(new DataColumn(fieldInfo.Name, colType));
                    }
                }

                DataRow dr = dtReturn.NewRow();

                if (oProps != null)
                {
                    foreach (PropertyInfo pi in oProps)
                    {
                        dr[pi.Name] = pi.GetValue(rec, null) ?? DBNull.Value;
                    }
                }
                if (oField != null)
                {
                    foreach (FieldInfo fieldInfo in oField)
                    {
                        dr[fieldInfo.Name] = fieldInfo.GetValue(rec) ?? DBNull.Value;
                    }
                }
                dtReturn.Rows.Add(dr);
            }
            return dtReturn;
        }

    }
}
