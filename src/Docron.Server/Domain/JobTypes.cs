using System.ComponentModel.DataAnnotations;

namespace Docron.Server.Domain;

public enum JobTypes
{
    [Display(Name = "None")]
    None = 0,
    [Display(Name = "Start container")]
    StartContainer = 1,
    [Display(Name = "Stop container")]
    StopContainer = 2,
}