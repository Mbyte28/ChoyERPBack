namespace ChoyERP.Domain.Entities.Contracting;

/// <summary>Tipo de sistema de refrigeración solicitado por el cliente.</summary>
public enum ProjectType
{
    ColdRoom = 0,
    FreezingTunnel = 1,
    CoolingTunnel = 2,
    Other = 3,
}

/// <summary>Prioridad comercial de una solicitud.</summary>
public enum RequestPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3,
}

/// <summary>Estado del ciclo de vida de una solicitud de proyecto.</summary>
public enum ProjectRequestStatus
{
    Received = 0,
    InEvaluation = 1,
    Quoted = 2,
    Accepted = 3,
    Rejected = 4,
    Cancelled = 5,
}

/// <summary>Estado del ciclo de vida de una cotización.</summary>
public enum QuoteStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Rejected = 3,
    Expired = 4,
    Cancelled = 5,
}

/// <summary>Estado del ciclo de vida de un contrato.</summary>
public enum ContractStatus
{
    Draft = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3,
}

/// <summary>Estado del ciclo de vida de un proyecto.</summary>
public enum ProjectStatus
{
    Planned = 0,
    InProgress = 1,
    Paused = 2,
    Completed = 3,
    Cancelled = 4,
}
