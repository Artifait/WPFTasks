
using TopNetwork.Services;

namespace WPFTasks.Core.Models.PcStore.Services
{
    public class PcPart
    {
        public string Title { get; set; } = string.Empty;
        public int Price { get; set; }
    }

    public class PcPartsService(IRepository<PcPart> repository)
    {
        private readonly IRepository<PcPart> _repository = repository;
        
        public PcPartsService RegisterPart(string title, int price)
        {
            RegisterPart(new() { Title = title, Price = price });
            return this;
        }

        public void UpdatePart(PcPart part)
        {
            RemoveUser(part.Title);
            _repository.Add(part);
        }

        public PcPart? SearchPcPart(string searchData)
        {
            var allParts = _repository.GetAll();

            var result = allParts.Where(part => part.Title.Contains(searchData, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
            return result;
        }

        public void RemoveUser(string title)
            => _repository.Remove(u => u.Title == title);

        public List<PcPart> GetAllParts()
            => _repository.GetAll();
        private void RegisterPart(PcPart part)
            => _repository.Add(part);
    }
}
