﻿// <auto-generated />
using System;
using FlomtManager.Data.EF.SQLite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FlomtManager.Data.EF.SQLite.Migrations
{
    [DbContext(typeof(SQLiteAppDb))]
    partial class SQLiteAppDbModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.1");

            modelBuilder.Entity("FlomtManager.Data.EF.Entities.DeviceDefinitionEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ushort>("CRC")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<byte[]>("CurrentParameterLineDefinition")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<ushort>("CurrentParameterLineDefinitionStart")
                        .HasColumnType("INTEGER");

                    b.Property<byte>("CurrentParameterLineLength")
                        .HasColumnType("INTEGER");

                    b.Property<byte>("CurrentParameterLineNumber")
                        .HasColumnType("INTEGER");

                    b.Property<ushort>("CurrentParameterLineStart")
                        .HasColumnType("INTEGER");

                    b.Property<ushort>("DescriptionStart")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DeviceId")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("IntegralParameterLineDefinition")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<ushort>("IntegralParameterLineDefinitionStart")
                        .HasColumnType("INTEGER");

                    b.Property<byte>("IntegralParameterLineLength")
                        .HasColumnType("INTEGER");

                    b.Property<byte>("IntegralParameterLineNumber")
                        .HasColumnType("INTEGER");

                    b.Property<ushort>("IntegralParameterLineStart")
                        .HasColumnType("INTEGER");

                    b.Property<ushort>("ParameterDefinitionNumber")
                        .HasColumnType("INTEGER");

                    b.Property<ushort>("ParameterDefinitionStart")
                        .HasColumnType("INTEGER");

                    b.Property<ushort>("ProgramVersionStart")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Updated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("Id");

                    b.HasIndex("DeviceId")
                        .IsUnique();

                    b.ToTable("DeviceDefinitions");
                });

            modelBuilder.Entity("FlomtManager.Data.EF.Entities.DeviceEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Address")
                        .HasColumnType("TEXT");

                    b.Property<int>("BaudRate")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ConnectionType")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<int>("DataBits")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DeviceDefinitionId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("IpAddress")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Parity")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Port")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PortName")
                        .HasColumnType("TEXT");

                    b.Property<string>("SerialCode")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<byte>("SlaveId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("StopBits")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Updated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("Id");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("FlomtManager.Data.EF.Entities.ParameterEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<int>("DeviceId")
                        .HasColumnType("INTEGER");

                    b.Property<ushort>("ErrorMask")
                        .HasColumnType("INTEGER");

                    b.Property<byte>("IntegrationNumber")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(4)
                        .HasColumnType("TEXT");

                    b.Property<byte>("Number")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Unit")
                        .IsRequired()
                        .HasMaxLength(6)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Updated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("Id");

                    b.HasIndex("DeviceId");

                    b.ToTable("Parameters");
                });

            modelBuilder.Entity("FlomtManager.Data.EF.Entities.DeviceDefinitionEntity", b =>
                {
                    b.HasOne("FlomtManager.Data.EF.Entities.DeviceEntity", "Device")
                        .WithOne("DeviceDefinition")
                        .HasForeignKey("FlomtManager.Data.EF.Entities.DeviceDefinitionEntity", "DeviceId");

                    b.Navigation("Device");
                });

            modelBuilder.Entity("FlomtManager.Data.EF.Entities.ParameterEntity", b =>
                {
                    b.HasOne("FlomtManager.Data.EF.Entities.DeviceEntity", "Device")
                        .WithMany("Parameters")
                        .HasForeignKey("DeviceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Device");
                });

            modelBuilder.Entity("FlomtManager.Data.EF.Entities.DeviceEntity", b =>
                {
                    b.Navigation("DeviceDefinition");

                    b.Navigation("Parameters");
                });
#pragma warning restore 612, 618
        }
    }
}
