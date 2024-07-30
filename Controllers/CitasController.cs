using Microsoft.AspNetCore.Mvc;
using MediFinder_Backend.Models;
using Microsoft.EntityFrameworkCore;
using static MediFinder_Backend.ModelosEspeciales.RegistrarCita;
using static MediFinder_Backend.ModelosEspeciales.RegistrarMedico;

namespace MediFinder_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitasController : ControllerBase
    {
        //Variable de ccontexto de BD
        private readonly MedifinderContext _baseDatos;

        //Contructor del controlador
        public CitasController(MedifinderContext baseDatos)
        {
            this._baseDatos = baseDatos;
        }

        //Registrar Cita --------------------------------------------------------
        [HttpPost]
        [Route("RegistrarCita")]
        public async Task<ActionResult> RegistrarCita([FromBody] CitaDTO cita)
        {
            //Valida que el modelo recibido este correcto
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                //Validar que el Id del médico recibido si existe en la BD
                var existeMedico = await _baseDatos.Medicos.FirstOrDefaultAsync(e => e.Id == cita.IdMedico);
                if (existeMedico == null)
                {
                    return BadRequest($"El médico ingresado no existe. La cita no ha sido agendada.");
                }

                //Validar que el id del paciente recibido exista en la BD
                var existePaciente = await _baseDatos.Paciente.FirstOrDefaultAsync(e => e.Id == cita.IdPaciente);
                if (existePaciente == null)
                {
                    return BadRequest($"El paciente ingresado no existe. La cita no ha sido agendada.");
                }

                // Validar que las fechas recibidas no estén volteadas
                if (cita.FechaInicio.HasValue && cita.FechaFin.HasValue)
                {
                    if (cita.FechaFin < cita.FechaInicio)
                    {
                        return BadRequest("La fecha de fin no puede ser anterior a la fecha de inicio.");
                    }
                }
                else
                {
                    return BadRequest("Las fechas de inicio y fin son requeridas.");
                }

                //Formateamos el modelo de la cita
                var citaNueva = new Citum
                {
                    IdPaciente = cita.IdPaciente,
                    IdMedico = cita.IdMedico,
                    FechaInicio = cita.FechaInicio,
                    FechaFin = cita.FechaFin,
                    Descripcion = cita.Descripcion,
                    Estatus = "0",
                    FechaCancelacion = cita.FechaCancelacion,
                    MotivoCancelacion = cita.MotivoCancelacion
                };

                // Guardar la cita en la base de datos
                _baseDatos.Cita.Add(citaNueva);
                await _baseDatos.SaveChangesAsync();

                return Ok(new { message = "Cita registrada correctamente", citaNueva.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        //Modificar Cita --------------------------------------------------------
        [HttpPut]
        [Route("ModificarCita/{id}")]
        public async Task<IActionResult> ModificarCita(int id, [FromBody] CitaDTO citaDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                //Validar que el Id de la cita recibido si existe en la BD
                var existeCita = await _baseDatos.Cita.FirstOrDefaultAsync(e => e.Id == id);
                if (existeCita == null)
                {
                    return BadRequest($"No existe ningún registro de la cita recibida.");
                }

                //Validar que el Id del médico recibido si existe en la BD
                var existeMedico = await _baseDatos.Medicos.FirstOrDefaultAsync(e => e.Id == citaDTO.IdMedico);
                if (existeMedico == null)
                {
                    return BadRequest($"El médico ingresado no existe. La cita no ha sido modificada.");
                }

                //Validar que el id del paciente recibido exista en la BD
                var existePaciente = await _baseDatos.Paciente.FirstOrDefaultAsync(e => e.Id == citaDTO.IdPaciente);
                if (existePaciente == null)
                {
                    return BadRequest($"El paciente ingresado no existe. La cita no ha sido modificada.");
                }

                // Validar que las fechas recibidas no estén volteadas
                if (citaDTO.FechaInicio.HasValue && citaDTO.FechaFin.HasValue)
                {
                    if (citaDTO.FechaFin < citaDTO.FechaInicio)
                    {
                        return BadRequest("La fecha de fin no puede ser anterior a la fecha de inicio.");
                    }
                }
                else
                {
                    return BadRequest("Las fechas de inicio y fin son requeridas.");
                }

                //Actualizar la informacion de la cita

                existeCita.IdPaciente = citaDTO.IdPaciente;
                existeCita.IdMedico = citaDTO.IdMedico;
                existeCita.FechaInicio = citaDTO.FechaInicio;
                existeCita.FechaFin = citaDTO.FechaFin;
                existeCita.Descripcion = citaDTO.Descripcion;
                existeCita.Estatus = "0";
                existeCita.FechaCancelacion = citaDTO.FechaCancelacion;
                existeCita.MotivoCancelacion = citaDTO.MotivoCancelacion;

                //Guardamos la informacion nueva
                await _baseDatos.SaveChangesAsync();

                //retornamos mensaje de confirmacion
                return Ok(new { message = $"La cita con el Id {existeCita.Id} ha sido modificada correctamente." });

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        //Obtener listado de citas por paciente --------------------------------------------------------
        [HttpGet]
        [Route("ObtenerCitasPacientes/{idPaciente}")]
        public async Task<IActionResult> ObtenerCitasPacientes(int idPaciente)
        {
            try
            {
                //Validar que el id del paciente recibido exista en la BD
                var existePaciente = await _baseDatos.Paciente.FirstOrDefaultAsync(e => e.Id == idPaciente);
                if (existePaciente == null)
                {
                    return BadRequest($"El paciente ingresado no existe. La cita no ha sido modificada.");
                }

                //Consultamos las listas y formateamos estructura de respuesta
                var listaCitasPaciente = await _baseDatos.Cita
                .Where(a => a.IdPaciente == idPaciente)
                .Include(p => p.IdPacienteNavigation)
                .Include(m => m.IdMedicoNavigation)
                .Select(c => new CitaInfoDto
                {
                    Id = c.Id,
                    IdPaciente = c.IdPaciente,
                    IdMedico = c.IdMedico,
                    FechaInicio = c.FechaInicio,
                    FechaFin = c.FechaFin,
                    Descripcion = c.Descripcion,
                    Estatus = c.Estatus,
                    FechaCancelacion = c.FechaCancelacion,
                    MotivoCancelacion = c.MotivoCancelacion,
                    NombrePaciente = c.IdPacienteNavigation.Nombre,
                    ApellidoPaciente = c.IdPacienteNavigation.Apellido,
                    NombreMedico = c.IdMedicoNavigation.Nombre,
                    ApellidoMedico = c.IdMedicoNavigation.Apellido
                })
                .ToListAsync();

                //Retornamos resultados
                return Ok(listaCitasPaciente);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        //Obtener listado de citas por medico --------------------------------------------------------
        [HttpGet]
        [Route("ObtenerCitasMedicos/{idMedico}")]
        public async Task<IActionResult> ObtenerCitasMedico(int idMedico)
        {
            try
            {
                //Validar que el Id del médico recibido si existe en la BD
                var existeMedico = await _baseDatos.Medicos.FirstOrDefaultAsync(e => e.Id == idMedico);
                if (existeMedico == null)
                {
                    return BadRequest($"El médico ingresado no existe.");
                }

                //Consultamos las listas y formateamos estructura de respuesta
                var listaCitasPacientes = await _baseDatos.Cita
                .Where(c => c.IdMedico == idMedico)
                .Include(p => p.IdPacienteNavigation)
                .Include(m => m.IdMedicoNavigation)
                .Select(c => new CitaInfoDto
                {
                    Id = c.Id,
                    IdPaciente = c.IdPaciente,
                    IdMedico = c.IdMedico,
                    FechaInicio = c.FechaInicio,
                    FechaFin = c.FechaFin,
                    Descripcion = c.Descripcion,
                    Estatus = c.Estatus,
                    FechaCancelacion = c.FechaCancelacion,
                    MotivoCancelacion = c.MotivoCancelacion,
                    NombrePaciente = c.IdPacienteNavigation.Nombre,
                    ApellidoPaciente = c.IdPacienteNavigation.Apellido,
                    NombreMedico = c.IdMedicoNavigation.Nombre,
                    ApellidoMedico = c.IdMedicoNavigation.Apellido
                })
                .ToListAsync();

                //Retornamos resultados
                return Ok(listaCitasPacientes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        //Confirmar cita por medico --------------------------------------------------------
        [HttpPut]
        [Route("ConfirmarCitaMedico/{id}")]
        public async Task<IActionResult> ConfirmarCitaMedico(int id)
        {
            try
            {
                //Validar que el Id de la cita recibido si existe en la BD
                var existeCita = await _baseDatos.Cita.FirstOrDefaultAsync(e => e.Id == id);
                if (existeCita == null)
                {
                    return BadRequest($"No existe ningún registro de la cita recibida.");
                }

                //Validar que la cita no este cancelada
                if (existeCita.Estatus == "3" || existeCita.Estatus == "4")
                {
                    return BadRequest($"No se puede confirmar la cita porque tiene un estatus de cancelada.");
                }

                //Validar que la cita no se haya finalizado
                if (existeCita.Estatus == "5")
                {
                    return BadRequest($"No se puede confirmar la cita porque tiene un estatus de terminada.");
                }

                //Cambiamos el estatus a confirmada por medico
                existeCita.Estatus = "1"; 

                //Guardamos la informacion nueva
                await _baseDatos.SaveChangesAsync();

                //retornamos mensaje de confirmacion
                return Ok(new { message = $"Cita confirmada con éxito." });

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        //Confirmar cita por paciente --------------------------------------------------------
        [HttpPut]
        [Route("ConfirmarCitaPaciente/{id}")]
        public async Task<IActionResult> ConfirmarCitaPaciente(int id)
        {
            try
            {
                //Validar que el Id de la cita recibido si existe en la BD
                var existeCita = await _baseDatos.Cita.FirstOrDefaultAsync(e => e.Id == id);
                if (existeCita == null)
                {
                    return BadRequest($"No existe ningún registro de la cita recibida.");
                }

                //Validar que la cita no este cancelada
                if (existeCita.Estatus == "3" || existeCita.Estatus == "4")
                {
                    return BadRequest($"No se puede confirmar la cita porque tiene un estatus de cancelada.");
                }

                //Validar que la cita no se haya finalizado
                if (existeCita.Estatus == "5")
                {
                    return BadRequest($"No se puede confirmar la cita porque tiene un estatus de terminada.");
                }

                //Cambiamos el estatus a confirmada por paciente
                existeCita.Estatus = "2";

                //Guardamos la informacion nueva
                await _baseDatos.SaveChangesAsync();

                //retornamos mensaje de confirmacion
                return Ok(new { message = $"Cita confirmada con éxito." });

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        //Cancelar cita por medico --------------------------------------------------------
        [HttpPut]
        [Route("CancelarCitaMedico/{id}")]
        public async Task<IActionResult> CancelarCitaMedico(int id, [FromBody] CancelarCitaDTO cancelarCitaDTO)
        {
            try
            {
                //Validar que el Id de la cita recibido si existe en la BD
                var existeCita = await _baseDatos.Cita.FirstOrDefaultAsync(e => e.Id == id);
                if (existeCita == null)
                {
                    return BadRequest($"No existe ningún registro de la cita recibida.");
                }

                //Validar que la cita no este cancelada por paciente
                if (existeCita.Estatus == "4")
                {
                    return BadRequest($"No se puede cancelar la cita porque ya esta cancelada por el paciente.");
                }

                //Validar que la cita no se haya finalizado
                if (existeCita.Estatus == "5")
                {
                    return BadRequest($"No se puede cancelar la cita porque tiene un estatus de terminada.");
                }

                //Cambiamos el estatus a confirmada por medico
                existeCita.Estatus = "3";
                existeCita.MotivoCancelacion = cancelarCitaDTO.MotivoCancelacion;
                existeCita.FechaCancelacion = DateTime.Now;

                //Guardamos la informacion nueva
                await _baseDatos.SaveChangesAsync();

                //retornamos mensaje de confirmacion
                return Ok(new { message = $"Cita cancelada con éxito." });

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        //Confirmar cita por paciente --------------------------------------------------------
        [HttpPut]
        [Route("CancelarCitaPaciente/{id}")]
        public async Task<IActionResult> CancelarCitaPaciente(int id, [FromBody] CancelarCitaDTO cancelarCitaDTO)
        {
            try
            {
                //Validar que el Id de la cita recibido si existe en la BD
                var existeCita = await _baseDatos.Cita.FirstOrDefaultAsync(e => e.Id == id);
                if (existeCita == null)
                {
                    return BadRequest($"No existe ningún registro de la cita recibida.");
                }

                //Validar que la cita no este cancelada por el médico
                if (existeCita.Estatus == "3")
                {
                    return BadRequest($"No se puede cancelar la cita porque ya esta cancelada por el médico.");
                }

                //Validar que la cita no se haya finalizado
                if (existeCita.Estatus == "5")
                {
                    return BadRequest($"No se puede cancelar la cita porque tiene un estatus de terminada.");
                }

                //Cambiamos el estatus a confirmada por paciente
                existeCita.Estatus = "4";
                existeCita.MotivoCancelacion = cancelarCitaDTO.MotivoCancelacion;
                existeCita.FechaCancelacion = DateTime.Now;

                //Guardamos la informacion nueva
                await _baseDatos.SaveChangesAsync();

                //retornamos mensaje de confirmacion
                return Ok(new { message = $"Cita cancelada con éxito." });

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

    }
}
