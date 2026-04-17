using AgendamientoCitas.Dtos;
using FluentValidation;

namespace AgendamientoCitas.Validaciones
{
    public class RecuperarPasswordDTOValidador : AbstractValidator<RecuperarPasswordDTO>
    {
        public RecuperarPasswordDTOValidador()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(Utilidades.CampoRequeridoMensaje)
                .MaximumLength(256).WithMessage(Utilidades.MaximumLengthMensaje)
                .EmailAddress().WithMessage(Utilidades.EmailMensaje);

            RuleFor(x => x.PasswordNueva)
                .NotEmpty().WithMessage(Utilidades.CampoRequeridoMensaje);
        }
    }
}
