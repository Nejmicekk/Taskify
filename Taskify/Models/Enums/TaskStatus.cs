namespace Taskify.Models.Enums;

public enum TaskStatus
{
    // 0. Čeká na schválení (připraveno pro budoucí moderaci)
    PendingApproval = 0,

    // 1. Veřejně viditelný úkol, čeká na dobrovolníka
    Open = 1,

    // 2. Někdo na úkolu pracuje
    InProgress = 2,

    // 3. Hotovo
    Completed = 3,
    
    // 4. Zrušeno/archivováno
    Archived = 4
}