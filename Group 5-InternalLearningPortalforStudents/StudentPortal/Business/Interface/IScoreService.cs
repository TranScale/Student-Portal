using StudentPortal.Models;

namespace StudentPortal.Business.Interface
{
    public interface IScoreService
    {
        Task<Score> AddScore(Score score);
        Task<Score> UpdateScore(Score score);
        Task<List<Score>> GetScoresByStudent(int studentId);
        Task<List<Score>> GetScoresBySection(int sectionId);
    }

}

