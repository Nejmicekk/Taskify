namespace Taskify.Models.Enums;

public enum NotificationType
{
    General,
    TaskUpdate,    // Přijetí, dokončení, zrušení
    TaskResult,    // Schválení, smazání
    Achievement,   // Level UP, reputace (nechodí na mail)
    Security       // Blokování účtu
}
