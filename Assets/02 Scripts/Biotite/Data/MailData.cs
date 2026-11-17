
using System;
using System.Collections.Generic;
using BackEnd;


//매일에 필요한 데이터들을 정의 합니다.

public class MailCurrencyData
{
    public string chartFileName;
    public string currencyName;
    public string currencyInfo;
    public string itemID;
    public int amount;
}

public class MailData
{
    public PostType postType;
    public string title;
    public string content;
    public DateTime expirationDate;
    public DateTime reservationDate;
    public DateTime sentDate;
    public string nickname;
    public string inDate; 
    public List<MailCurrencyData> mailRewardItemDatas;
}
