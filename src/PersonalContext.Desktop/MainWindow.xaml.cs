using System.Globalization;
using System.IO;
using System.Windows;
using Microsoft.Data.Sqlite;
using PersonalContext.Storage;

namespace PersonalContext.Desktop;

public partial class MainWindow : Window
{
    private bool _started;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_started) return;
        _started = true;

        try
        {
            var store = new LocalStore();
            DatabasePathText.Text = store.DatabasePath;
            // LocalStore is synchronous: initialization and reading both run off the UI thread.
            // GetConversations initializes the store and disposes its SQLite connections.
            var count = await Task.Run(() => store.GetConversations().Count);
            ConversationCountText.Text = count.ToString(CultureInfo.CurrentCulture);
            StatusText.Text = "Base local disponible.";
        }
        catch (Exception exception)
        {
            StatusText.Text = "No se pudo abrir o leer la base local.";
            ConversationCountText.Text = "No disponible";
            ErrorText.Text = DescribeError(exception) +
                " No se ha borrado ni sustituido la base. Cierra la aplicación y vuelve a intentarlo tras resolver el problema.";
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private static string DescribeError(Exception exception) => exception switch
    {
        UnauthorizedAccessException => "No hay permiso para acceder a la ruta indicada. Revisa los permisos de la carpeta.",
        NotSupportedException => "La versión de la base no es compatible con esta aplicación.",
        SqliteException { SqliteErrorCode: 5 or 6 } => "La base está ocupada. Cierra las otras aplicaciones que la estén usando.",
        SqliteException { SqliteErrorCode: 11 or 26 } => "El archivo no es una base SQLite válida o está dañado. Conserva el archivo y recupera una copia de seguridad.",
        SqliteException { SqliteErrorCode: 3 or 8 or 14 } => "No se puede acceder a la base. Revisa la ruta y los permisos de la carpeta.",
        SqliteException { SqliteErrorCode: 13 } => "No hay espacio suficiente en el disco para abrir la base.",
        IOException => "No se puede acceder al archivo. Revisa la ruta, el espacio libre y si otro programa lo está usando.",
        DllNotFoundException or BadImageFormatException or TypeInitializationException => "No se pudo cargar el almacenamiento local. Usa la carpeta publicada completa para Windows x64.",
        _ => "Se produjo un error inesperado al abrir el almacenamiento local. Comprueba que tienes la carpeta publicada completa."
    };
}
