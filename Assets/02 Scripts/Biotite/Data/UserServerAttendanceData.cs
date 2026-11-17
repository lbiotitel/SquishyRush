using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using System;



//유저의 출석체크 데이터 입니다.
public class UserServerAttendanceData : UserServerDataBase
{
    private bool isAttendanceCheckedToday;
    //오늘 출석을 했는지 정보입니다.
    public bool IsAttendanceCheckedToday
    {
        get =>  isAttendanceCheckedToday;
        set
        {
            IsDirty = true;
            isAttendanceCheckedToday = value;
        }
    }

    private bool[] attendanceChecked;
    public bool[] AttendanceChecked => attendanceChecked;

    public void ClearAttendanceChecked()
    {
        attendanceChecked = new bool[GlobalDefines.MAX_VALUE.MAX_DAILY_REWARD_COUNT];
        IsDirty = true;
    }

    public void SetAttendanceChecked(int index, bool value)
    {
        if (attendanceChecked == null || index < 0 || index >= attendanceChecked.Length)
            return;

        attendanceChecked[index] = value;
        IsDirty = true;   
    }

    public override void Initialize()
    {
        tableId = GlobalDefines.DataTableName.USER_ATTENDANCE_DATA_TABLE;
    }


    protected override void ParsingData(JsonData data)
    {
        // inDate
        if (data.Keys.Contains("inDate") && data["inDate"] != null)
            InDate = data["inDate"].ToString();


        // isAttendanceCheckedToday
        if (data.Keys.Contains("isAttendanceCheckedToday") && data["isAttendanceCheckedToday"] != null)
        {
            string jsonString = data["isAttendanceCheckedToday"].ToString();
            if (bool.TryParse(jsonString, out bool isChecked))
            {
                isAttendanceCheckedToday = isChecked;
            }
        }

        // AttendanceChecked
        if (data.Keys.Contains("AttendanceChecked") && data["AttendanceChecked"] != null)
        {
            JsonData arrData = data["AttendanceChecked"];

            if (arrData.IsArray)
            {
                attendanceChecked = new bool[arrData.Count];

                for (int i = 0; i < arrData.Count; i++)
                {
                    if (arrData[i] != null && bool.TryParse(arrData[i].ToString(), out bool result))
                    {
                        attendanceChecked[i] = result;
                    }
                    else
                    {
                        attendanceChecked[i] = false;
                    }
                }
            }
        }
    }


    public override void SetDefaultData()
    {
        isAttendanceCheckedToday = false;
        attendanceChecked = new bool[GlobalDefines.MAX_VALUE.MAX_DAILY_REWARD_COUNT];
    }


    public override Param ToParam()
    {
        Param param = new Param();
        param.Add("isAttendanceCheckedToday", isAttendanceCheckedToday);
        param.Add("AttendanceChecked", attendanceChecked);
        return param;   
    }
    
}
