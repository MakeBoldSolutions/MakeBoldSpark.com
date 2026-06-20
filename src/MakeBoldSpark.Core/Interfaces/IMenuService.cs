using MakeBoldSpark.Core.Models.EditModels;

namespace MakeBoldSpark.Core.Interfaces;
/// <summary>
/// 
/// </summary>
public interface IMenuService
{
    IEnumerable<Models.MenuModel> GetAllMenuItems();
    List<Models.MenuModel> Save(List<Models.MenuModel> saveMenus);
    Models.MenuModel Save(Models.MenuModel saveItem);
    List<Models.MenuModel> GetSiteMenu(int DomainId);
    Task<MenuEditModel> GetMenuEditAsync(int Id);
    Task<bool> DeleteMenuAsync(int Id);

}
