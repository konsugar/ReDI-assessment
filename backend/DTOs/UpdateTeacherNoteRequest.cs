using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.DTOs;

public class UpdateTeacherNoteRequest
{
    [MaxLength(2000)]
    public string TeacherNote { get; set; } = string.Empty;
}