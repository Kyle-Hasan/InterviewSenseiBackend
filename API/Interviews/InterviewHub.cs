using API.Base;
using Microsoft.AspNetCore.SignalR;

namespace API.Interviews;

public class InterviewHub: BaseHub
{
  public InterviewHub() : base("interviews") { }
}