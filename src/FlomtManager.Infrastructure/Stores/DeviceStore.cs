﻿using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Models;

namespace FlomtManager.Infrastructure.Stores;

internal sealed class DeviceStore : Store<Device>, IDeviceStore;
