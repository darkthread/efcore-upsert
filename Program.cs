using CRUDExample.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

bool enableLog = false;
// 使用記憶體中的 SQLite 資料庫，來去不留痕跡
// https://blog.darkthread.net/blog/ef-core-test-with-in-memory-db/
var cn = new SqliteConnection("Data Source=:memory:");
cn.Open();
var dbOpt = new DbContextOptionsBuilder<JournalDbContext>()
    .UseSqlite(cn)
    // 設定可動態開關的 Log 輸出以觀察 SQL 語法
    .LogTo(s => {
        if (enableLog) Console.WriteLine(s);
    }, Microsoft.Extensions.Logging.LogLevel.Information)
    // 連同寫入資料庫的參數一起顯示，正式環境需留意個資或敏感資料寫入Log
    .EnableSensitiveDataLogging() 
    .Options;
var dbCtx = new JournalDbContext(dbOpt);

Action<string> print = (s) => {
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(s);
    Console.ResetColor();
};

//準備資料
dbCtx.Database.EnsureCreated();
dbCtx.Records.Add(new DailyRecord{
    Date = new DateTime(2022, 1, 1),
    EventSummary = "BeforeUpdate",
    Remark = "",
    User = "Jeffrey"
});
dbCtx.SaveChanges();

// 測試更新(同日期資料已存在)
print("測試更新");
Upsert(new DailyRecord{
    Date = new DateTime(2022, 1, 1),
    EventSummary = "AfterUpdate",
    Remark = "",
    User = "darkthread"
});

// 測試新增(無相同日期資料)
print("測試新增");
Upsert(new DailyRecord{
    Date = new DateTime(2022, 1, 2),
    EventSummary = "NewEntry",
    Remark = "Hello",
    User = "Jeffrey"
});


void Upsert(DailyRecord record) {
    var exist = dbCtx!.Records.FirstOrDefault(o => o.Date == record.Date);
    if (exist == null) {
        dbCtx.Records.Add(record);
    } else {
        //Id 為自動跳號，將待更新資料I Id 設成一致
        //record.Id = exist.Id;
        dbCtx.Entry(exist).CurrentValues.SetValues(record);
    }
    enableLog = true;
    dbCtx.SaveChanges();
    enableLog = false;
}