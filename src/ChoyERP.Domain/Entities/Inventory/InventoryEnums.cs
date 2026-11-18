namespace ChoyERP.Domain.Entities.Inventory;

/// <summary>Clasificación del artículo dentro del almacén.</summary>
public enum InventoryCategory
{
    Spare = 0,
    Component = 1,
    Equipment = 2,
    Supply = 3,
    Tool = 4,
    Other = 5,
}

/// <summary>Unidad de medida utilizada para controlar las existencias.</summary>
public enum InventoryUnit
{
    Unit = 0,
    Kilogram = 1,
    Liter = 2,
    Meter = 3,
    Box = 4,
    Set = 5,
    Other = 6,
}

/// <summary>Tipo de movimiento de inventario.</summary>
public enum InventoryMovementType
{
    /// <summary>Entrada de artículos al almacén.</summary>
    Entry = 0,

    /// <summary>Salida de artículos, normalmente asociada a un proyecto.</summary>
    Exit = 1,

    /// <summary>Ajuste por conteo físico; la cantidad es la existencia resultante.</summary>
    Adjustment = 2,
}
