using System.ComponentModel.DataAnnotations;

namespace MediFinder_Backend.ModelosEspeciales
{
    public class RegistrarPacientes
    {
        public class LoginDTO
        {
            [Required(ErrorMessage = "El campo Email es requerido")]
            public string Email { get; set; }

            [Required(ErrorMessage = "El campo Contraseña es requerido")]
            public string Contrasena { get; set; }
        }
    }
}
