// SCENARIO 5 — "Budget approval workflow: submit -> approve/reject -> lock."
using System.Net.Mail;

namespace Prophix.Review.Scenarios;

public class Budget
{
    public int Id;
    public string Status = "Draft";
    public string Owner;
    public decimal Total;
    public DateTime? ApprovedOn;
}

public class ApprovalService
{
    public static List<Budget> Budgets = new();

    public Budget Approve(int id, string user, bool force, bool notify)
    {
        Budget budget = null;
        try { budget = Budgets.First(b => b.Id == id); }
        catch { return null; }

        if (budget.Owner == user && !force)
            throw new Exception("Cannot approve own budget");

        budget.Status = "Approved";
        budget.ApprovedOn = DateTime.Now;

        if (notify)
        {
            var smtp = new SmtpClient("smtp.prophix.local");
            smtp.Send("noreply@x.com", budget.Owner, "Approved", "Budget " + id + " approved by " + user);
        }
        Console.WriteLine("Approved budget " + id + " total " + budget.Total);
        return budget;
    }
}
