using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;

namespace AgendamientoCitas.Servicios
{
    public class ServicioEmail : IServicioEmail
    {
        private readonly EmailSettings settings;

        public ServicioEmail(IOptions<EmailSettings> options)
        {
            settings = options.Value;
        }

        public async Task EnviarRecuperacionPasswordAsync(string destinatario, string token, CancellationToken cancellationToken = default)
        {
            ValidarConfiguracion();

            var resetUrl = ConstruirUrlRecuperacion(destinatario, token);
            var htmlResetUrl = HtmlEncoder.Default.Encode(resetUrl);

            using var mensaje = new MailMessage
            {
                From = new MailAddress(settings.FromEmail, settings.FromName),
                Subject = "Recuperacion de clave - Mi Agenda",
                Body = $"""
                    <p>Hola,</p>
                    <p>Recibimos una solicitud para recuperar el acceso a tu cuenta.</p>
                    <p>
                        <a href="{htmlResetUrl}">Restablecer mi clave</a>
                    </p>
                    <p>Si no solicitaste este cambio, puedes ignorar este mensaje.</p>
                    """,
                IsBodyHtml = true
            };

            mensaje.To.Add(destinatario);

            await EnviarAsync(mensaje, cancellationToken);
        }

        public async Task EnviarPasswordTemporalAsync(string destinatario, string passwordTemporal, CancellationToken cancellationToken = default)
        {
            ValidarConfiguracion();

            using var mensaje = new MailMessage
            {
                From = new MailAddress(settings.FromEmail, settings.FromName),
                Subject = "Clave temporal - Mi Agenda",
                Body = $"""
                    <p>Hola,</p>
                    <p>Un administrador genero una clave temporal para tu cuenta.</p>
                    <p><strong>Clave temporal:</strong> {HtmlEncoder.Default.Encode(passwordTemporal)}</p>
                    <p>Al iniciar sesion deberas cambiarla por una clave nueva.</p>
                    <p>Si no solicitaste ayuda, contacta al administrador.</p>
                    """,
                IsBodyHtml = true
            };

            mensaje.To.Add(destinatario);

            await EnviarAsync(mensaje, cancellationToken);
        }

        private async Task EnviarAsync(MailMessage mensaje, CancellationToken cancellationToken)
        {
            using var smtp = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = settings.EnableSsl,
                Credentials = new NetworkCredential(settings.UserName, settings.Password)
            };

            using var registration = cancellationToken.Register(smtp.SendAsyncCancel);
            await smtp.SendMailAsync(mensaje);
        }

        private string ConstruirUrlRecuperacion(string email, string token)
        {
            var separator = settings.FrontendResetPasswordUrl.Contains('?') ? '&' : '?';
            return $"{settings.FrontendResetPasswordUrl}{separator}email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        }

        private void ValidarConfiguracion()
        {
            if (string.IsNullOrWhiteSpace(settings.Host) ||
                string.IsNullOrWhiteSpace(settings.UserName) ||
                string.IsNullOrWhiteSpace(settings.Password) ||
                string.IsNullOrWhiteSpace(settings.FromEmail))
            {
                throw new InvalidOperationException("La configuracion SMTP no esta completa.");
            }
        }
    }
}
