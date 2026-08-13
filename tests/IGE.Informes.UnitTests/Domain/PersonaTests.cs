using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class PersonaTests
{
    [Fact]
    public void Alta_identificada_con_nombre_y_dni()
    {
        var persona = new Persona(RolPersona.Sospechoso, "Juan Pérez", "30123456");

        Assert.Equal("Juan Pérez", persona.Nombre);
        Assert.Equal("30123456", persona.Dni);
        Assert.Equal(RolPersona.Sospechoso, persona.Rol);
    }

    [Fact]
    public void Alta_sin_identificar_se_acepta_con_solo_caracteristicas()
    {
        var persona = new Persona(RolPersona.Sospechoso, caracteristicas: "Hombre, 1.80m, campera negra");

        Assert.Null(persona.Nombre);
        Assert.Null(persona.Dni);
        Assert.Equal("Hombre, 1.80m, campera negra", persona.Caracteristicas);
    }

    [Fact]
    public void Alta_rechaza_persona_completamente_vacia()
    {
        Assert.Throws<ArgumentException>(() => new Persona(RolPersona.Testigo));
    }

    [Fact]
    public void Identificar_completa_nombre_y_dni_de_una_persona_sin_identificar()
    {
        var persona = new Persona(RolPersona.Sospechoso, caracteristicas: "Hombre, 1.80m");

        persona.Identificar("Juan Pérez", "30123456");

        Assert.Equal("Juan Pérez", persona.Nombre);
        Assert.Equal("30123456", persona.Dni);
    }

    [Fact]
    public void Identificar_rechaza_nombre_vacio()
    {
        var persona = new Persona(RolPersona.Sospechoso, caracteristicas: "Hombre, 1.80m");

        Assert.Throws<ArgumentException>(() => persona.Identificar(""));
    }

    [Fact]
    public void CambiarRol_actualiza_el_rol()
    {
        var persona = new Persona(RolPersona.Denunciante, "Juan Pérez");

        persona.CambiarRol(RolPersona.Testigo);

        Assert.Equal(RolPersona.Testigo, persona.Rol);
    }

    // HU-21 · Borrado lógico de Informe, Caso de Análisis, Vehículo y
    // Persona (docs/epic-01-gestion-informes.md), Característica "Borrado
    // lógico de una Persona": Eliminado/FechaEliminacion/Eliminar() todavía
    // no existen en el dominio — deben fallar en rojo (TDD) hasta
    // implementarse.

    [Fact]
    public void Eliminar_Persona_MarcaEliminadoYFechaEliminacion()
    {
        var persona = new Persona(RolPersona.Sospechoso, "Juan Pérez", "30123456");

        persona.Eliminar();

        Assert.True(persona.Eliminado);
        Assert.NotNull(persona.FechaEliminacion);
    }
}
