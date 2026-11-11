namespace BackendTemplate.Schedule.Jobs;

public interface IJob
{
    System.Threading.Tasks.Task Execute();
}