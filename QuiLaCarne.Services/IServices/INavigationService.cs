using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Services.IServices;
public interface INavigationService
{
    void ShowLogin();
    void ShowMenu();
    void ShowUsersPanel();
    void ShowTwoFactor();
    void CloseCurrentWindow();
    void ShowManagerPanel();
    void CloseLogin();
}
