namespace Taskify.Models.Enums;

public enum TaskStatus
{
    // 0. Čeká na schválení (připraveno pro budoucí moderaci)
    PendingApproval = 0,

    // 1. Veřejně viditelný úkol, čeká na dobrovolníka
    Open = 1,

    // 2. Někdo na úkolu pracuje
    InProgress = 2,
    
    // 3. Dobrovolník má hotovo, čeká se, až to autor úkolu schválí
    WaitingForReview = 3,

    // 3. Hotovo
    Completed = 4,
    
    // 4. Zrušeno/archivováno
    Archived = 5
}