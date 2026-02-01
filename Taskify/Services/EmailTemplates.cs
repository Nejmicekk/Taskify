using System.Text.Encodings.Web;

namespace Taskify.Services
{
    public static class EmailTemplates
    {
        public static string GetHtmlTemplate(string title, string message, string buttonText, string buttonUrl)
        {
            string primaryColor = "#0d6efd";
            string textColor = "#333333";
            string mutedColor = "#999999";
            
            string safeUrl = HtmlEncoder.Default.Encode(buttonUrl);

            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px; background-color: #ffffff;'>
                
                <h1 style='color: {primaryColor}; text-align: center;'>{title}</h1>
                
                <p style='font-size: 16px; color: {textColor}; line-height: 1.5;'>
                    {message}
                </p>
                
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{safeUrl}' 
                       style='background-color: {primaryColor}; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px; display: inline-block;'>
                       {buttonText}
                    </a>
                </div>
                
                <p style='font-size: 14px; color: #666;'>
                    Pokud tlačítko nefunguje, zkopírujte tento odkaz do prohlížeče:<br>
                    <a href='{safeUrl}' style='color: {primaryColor}; word-break: break-all;'>{safeUrl}</a>
                </p>
                
                <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;'>
                
                <p style='font-size: 12px; color: {mutedColor}; text-align: center;'>
                    © {DateTime.Now.Year} Taskify. Toto je automatická zpráva, neodpovídejte na ni.
                </p>
            </div>";
        }
    }
}