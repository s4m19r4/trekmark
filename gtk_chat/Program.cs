using System;
using Gtk;
using Chat_Core;
using System.Windows.Input;
using MessageType = Chat_Core.MessageType;

namespace gtk_chat
{
    internal class Program
    {
       

        public static void Main(string[] args)
        {
            var gtkPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gtk3-runtime", "bin");
            Environment.SetEnvironmentVariable("PATH", gtkPath + ";" + Environment.GetEnvironmentVariable("PATH"));


            Application.Init();

            // Создаем билдер и загружаем интерфейс из файла chat.glade
            var builder = new Builder();
            builder.AddFromFile("glade_window.glade"); // файл должен быть рядом с exe

            // Получаем главное окно по имени, заданному в Glade (например, "MainWindow")
            var window = (Window)builder.GetObject("main_window");

            var viewModel = new GtkViewModel(builder);

            // Подписываем событие закрытия окна (чтобы приложение завершилось)
            window.DeleteEvent += (o, e) => Application.Quit();

            // Показываем все виджеты окна
            window.ShowAll();

            Application.Run();
        }
    }
}
