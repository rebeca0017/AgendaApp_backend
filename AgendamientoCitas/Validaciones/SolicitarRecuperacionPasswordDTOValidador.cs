using AgendamientoCitas.Dtos;
using FluentValidation;

namespace AgendamientoCitas.Validaciones
{
    public class SolicitarRecuperacionPasswordDTOValidador : AbstractValidator<SolicitarRecuperacionPasswordDTO>
    {
        public SolicitarRecuperacionPasswordDTOValidador()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(Utilidades.CampoRequeridoMensaje)
                .MaximumLength(256).WithMessage(Utilidades.MaximumLengthMensaje)
                .EmailAddress().WithMessage(Utilidades.EmailMensaje);
        }
    }
}
