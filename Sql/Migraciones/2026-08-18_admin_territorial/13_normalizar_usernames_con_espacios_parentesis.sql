/*
    13. Normaliza el campo Usuario (login) para las cuentas que
    tenian espacios y/o parentesis (apodos de grupo tipo "Vane (Yo Lucho)").
    Se genera un username limpio a partir de NombreCompleto: primera
    letra del primer nombre + primer apellido, en minusculas, sin
    tildes ni caracteres especiales (mismo criterio ya usado en la
    limpieza de Usuarios.xlsx). Solo se toca la columna Usuario -- nada
    mas de la fila cambia.

    IMPORTANTE: esto cambia las credenciales de login reales de estas
    cuentas -- las personas afectadas deben ser avisadas del nuevo
    usuario para poder volver a entrar.

    Generado automaticamente, revisar el mapeo antes de ejecutar en
    otro ambiente distinto al de pruebas.
*/

SET QUOTED_IDENTIFIER ON;

UPDATE Usuario SET Usuario = 'rflorenciani' WHERE IdUsuario = 3116; -- era 'Roque Florenciani' (Roque Florenciani)
UPDATE Usuario SET Usuario = 'lpaola' WHERE IdUsuario = 3118; -- era 'Paola Díaz' (Letícia Paola)
UPDATE Usuario SET Usuario = 'roviedo' WHERE IdUsuario = 3126; -- era 'Ricardo OviedoP' (Ricardo Oviedo Portillo)
UPDATE Usuario SET Usuario = 'jsalvador' WHERE IdUsuario = 3155; -- era 'Jorge Salvador Lezcano Penayo' (Jorge Salvador Lezcano Penayo)
UPDATE Usuario SET Usuario = 'aquintana' WHERE IdUsuario = 3163; -- era 'Alber Quin' (Alberto Quintana)
UPDATE Usuario SET Usuario = 'gsilva' WHERE IdUsuario = 3165; -- era 'Gloria Martínez' (Gloria Silva)
UPDATE Usuario SET Usuario = 'maguirre' WHERE IdUsuario = 3166; -- era 'Marcelina Aguirre' (Marcelina Aguirre)
UPDATE Usuario SET Usuario = 'pivan' WHERE IdUsuario = 3167; -- era 'Paul Ivan Carrillo' (Paúl Ivan Carrillo)
UPDATE Usuario SET Usuario = 'ibernal' WHERE IdUsuario = 3168; -- era 'Idalina Bernal Maiz' (Idalina Bernal Maiz)
UPDATE Usuario SET Usuario = 'ehugo' WHERE IdUsuario = 3170; -- era 'Ever Hugo Arroquia' (Ever Hugo Arroquia)
UPDATE Usuario SET Usuario = 'cramon' WHERE IdUsuario = 3173; -- era 'Cecilio Hidalgo' (Cecilio Ramon Hidalgo)
UPDATE Usuario SET Usuario = 'nacosta' WHERE IdUsuario = 3174; -- era 'Nilda Acosta Godoy' (Nilda Acosta Godoy)
UPDATE Usuario SET Usuario = 'sraquel' WHERE IdUsuario = 3179; -- era 'Sani Raquel Maíz' (Sani Raquel Maíz)
UPDATE Usuario SET Usuario = 'tarmando' WHERE IdUsuario = 3186; -- era 'Tony Armando Paredes Gómez' (Tony Armando)
UPDATE Usuario SET Usuario = 'omaiz' WHERE IdUsuario = 3187; -- era 'Ovidio Maíz Gonzales' (Ovidio maíz)
UPDATE Usuario SET Usuario = 'ccarolina' WHERE IdUsuario = 3189; -- era 'Cintia Gonsalez' (Cintia Carolina Gosalez)
UPDATE Usuario SET Usuario = 'aquintin' WHERE IdUsuario = 3192; -- era 'Ariel Quintin Godoy Mora' (Ariel Quintin)
UPDATE Usuario SET Usuario = 'cpereira' WHERE IdUsuario = 3199; -- era 'CarlitoPereira (Margarita)' (Carlos Pereira (Margarita))
UPDATE Usuario SET Usuario = 'rchavez' WHERE IdUsuario = 3200; -- era 'Rita (Juan A Guanes)' (Rita Chávez (Juan A Guanes))
UPDATE Usuario SET Usuario = 'clemente' WHERE IdUsuario = 3201; -- era 'Clemente (Rafa)' (Clemente (Rafa))
UPDATE Usuario SET Usuario = 'vcornet' WHERE IdUsuario = 3203; -- era 'Vane (Yo Lucho)' (Vanessa Cornet (Yo Lucho))
UPDATE Usuario SET Usuario = 'wbeatriz' WHERE IdUsuario = 3204; -- era 'Wilma (Rafa)' (Wilma Beatriz Giménez Ortiz)
UPDATE Usuario SET Usuario = 'smoreno' WHERE IdUsuario = 3205; -- era 'Silvi (Anita Jara)' (Silvina Moreno (Ana Jara))
UPDATE Usuario SET Usuario = 'edenis' WHERE IdUsuario = 3207; -- era 'Evert (Ña Mechi)' (Evert Denis (Ña Mechi))
UPDATE Usuario SET Usuario = 'rrivas' WHERE IdUsuario = 3208; -- era 'Robert (Manolo)' (Roberto Rivas (Manolo))
UPDATE Usuario SET Usuario = 'miguel' WHERE IdUsuario = 3209; -- era 'Vane(Yolucho)' (miguel)
UPDATE Usuario SET Usuario = 'dcubilla' WHERE IdUsuario = 3212; -- era 'Diego (Ña Petro)' (Diego Cubilla (Ña Petro))
UPDATE Usuario SET Usuario = 'yisabel' WHERE IdUsuario = 3214; -- era 'Yanina Sarabia' (Yanina Isabel Sarabia Fernandez)
UPDATE Usuario SET Usuario = 'mrivas' WHERE IdUsuario = 3215; -- era 'Mercedes Rivas' (Mercedes Rivas)
UPDATE Usuario SET Usuario = 'gflorentin' WHERE IdUsuario = 3216; -- era 'Gerardo (Leo Dan)' (Gerardo Florentin (Leo Dan))
UPDATE Usuario SET Usuario = 'rcanet' WHERE IdUsuario = 3217; -- era 'Rei (German Mora)' (Reinaldo Canet (Prof German Mora))
UPDATE Usuario SET Usuario = 'oramon' WHERE IdUsuario = 3219; -- era 'Oscar (Leo Dan)' (Oscar Ramón López Morán)
UPDATE Usuario SET Usuario = 'vraul' WHERE IdUsuario = 3220; -- era 'Victor Raul Moreno' (Victor Raul Moreno)
UPDATE Usuario SET Usuario = 'mmartinez' WHERE IdUsuario = 3226; -- era 'Vane ( Yo lucho)' (Mirian Martinez)
UPDATE Usuario SET Usuario = 'avillalba2' WHERE IdUsuario = 3244; -- era 'anibal verificador' (Aníbal Villalba)
UPDATE Usuario SET Usuario = 'mcarolina' WHERE IdUsuario = 3247; -- era 'Carolina (Rafa)' (Mirtha Carolina Vaezquen)
UPDATE Usuario SET Usuario = 'lcarina' WHERE IdUsuario = 3248; -- era 'Carina(Rafa)' (Liz Carina Gómez Serafini)
UPDATE Usuario SET Usuario = 'mjosefina' WHERE IdUsuario = 3249; -- era 'Josefina(Rafa)' (María Josefina Aguayo Caballero)
UPDATE Usuario SET Usuario = 'avillalba3' WHERE IdUsuario = 3278; -- era 'anibal movilizador' (Anibal Villalba)
UPDATE Usuario SET Usuario = 'dledesma' WHERE IdUsuario = 3305; -- era 'Diego Ledesma' (Diego Ledesma)
UPDATE Usuario SET Usuario = 'carguello' WHERE IdUsuario = 3306; -- era 'Claudio Arguello' (Claudio Arguello)
UPDATE Usuario SET Usuario = 'caquino' WHERE IdUsuario = 3307; -- era 'Cándido Aquino' (Cándido Aquino)
UPDATE Usuario SET Usuario = 'ymartinez' WHERE IdUsuario = 3310; -- era 'Yenifer Martinez' (Yenifer Martinez Saucedo)
