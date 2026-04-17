using FluentValidation;
using AgendamientoCitas.Dtos;

namespace AgendamientoCitas.Validaciones
{
    public class CredencialesUsuarioDTOValidador : AbstractValidator<CredencialesUsuarioDTO>
    {
        public CredencialesUsuarioDTOValidador()
        {
            RuleFor(x => x.user).NotEmpty().WithMessage(Utilidades.CampoRequeridoMensaje)
                .MaximumLength(256).WithMessage(Utilidades.MaximumLengthMensaje)
                .EmailAddress().WithMessage(Utilidades.EmailMensaje);

            RuleFor(x => x.pass)
                .NotEmpty().WithMessage(Utilidades.CampoRequeridoMensaje);
        }
    }
}
