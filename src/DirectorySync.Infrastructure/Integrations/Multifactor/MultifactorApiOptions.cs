﻿using System.ComponentModel.DataAnnotations;

namespace DirectorySync.Infrastructure.Integrations.Multifactor;

public class MultifactorApiOptions
{
    [Required]
    public string Url { get; init; }

    [Required]
    public string Key { get; init; }

    [Required]
    public string Secret { get; init; }
}
