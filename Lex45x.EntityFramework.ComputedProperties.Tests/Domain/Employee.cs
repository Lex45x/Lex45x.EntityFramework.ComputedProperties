namespace Lex45x.EntityFramework.ComputedProperties.Tests.Domain;

public class Employee
{
    private readonly List<VacationRequest> vacationRequests = new();
    public Guid Id { get; }

    //Each employee has 25 vacation days in a given year.
    public int VacationAllowance { get; } = 25;

    //Vacation budget is a difference between Vacation Allowance and total days of pending and approved vacation requests
    [EfFriendly]
    public double VacationBudget => VacationAllowance - VacationRequests
        .Where(request => request.StartTime.Year == DateTime.UtcNow.Year)
        .Where(request =>
            request.State == VacationRequestState.Approved || request.State == VacationRequestState.Pending)
        .Sum(request => request.TotalDays);

    public IReadOnlyList<VacationRequest> VacationRequests => vacationRequests;

    public VacationRequest RequestVacation(DateTime startTime, DateTime endTime)
    {
        var vacationRequest = new VacationRequest(startTime, endTime);

        if (vacationRequest.TotalDays > VacationBudget)
        {
            throw new InvalidOperationException("Budget is exceeded!");
        }

        vacationRequests.Add(vacationRequest);

        return vacationRequest;
    }
}

public class VacationRequest
{
    public VacationRequest(Guid id, DateTime startTime, DateTime endTime, double totalDays, VacationRequestState state)
    {
        Id = id;
        StartTime = startTime;
        EndTime = endTime;
        TotalDays = totalDays;
        State = state;
    }

    public VacationRequest(DateTime startTime, DateTime endTime)
    {
        Id = Guid.NewGuid();
        StartTime = startTime;
        EndTime = endTime;
        TotalDays = (StartTime - EndTime).TotalDays;
        State = VacationRequestState.Pending;
    }

    public Guid Id { get; private set; }
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
    public double TotalDays { get; }
    public VacationRequestState State { get; }
}

public enum VacationRequestState
{
    Pending = 0,
    Approved = 1,
    Cancelled = 2
}