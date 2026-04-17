using AgendamientoCitas.Dtos;
using FluentValidation;

namespace AgendamientoCitas.Validaciones
{
    public class AdminAccionUsuarioDTOValidador : AbstractValidator<AdminAccionUsuarioDTO>
    {
        public AdminAccionUsuarioDTOValidador()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(Utilidades.CampoRequeridoMensaje)
                .MaximumLength(256).WithMessage(Utilidades.MaximumLengthMensaje)
                .EmailAddress().WithMessage(Utilidades.EmailMensaje);
        }
    }
}
