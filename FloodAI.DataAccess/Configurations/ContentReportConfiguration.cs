using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class ContentReportConfiguration : IEntityTypeConfiguration<ContentReport>
{
    public void Configure(EntityTypeBuilder<ContentReport> builder)
    {
        builder.ToTable("content_reports", table =>
        {
            table.HasCheckConstraint("ck_content_reports_single_target", "(post_id IS NOT NULL) <> (comment_id IS NOT NULL)");
            table.HasCheckConstraint("ck_content_reports_resolution_details",
                "(resolved_at IS NULL AND resolution_reason IS NULL) OR " +
                "(resolved_at IS NOT NULL AND resolution_reason IS NOT NULL AND btrim(resolution_reason) <> '' " +
                "AND resolved_by IS NOT NULL AND status IN ('RESOLVED', 'DISMISSED'))");
        });

        builder.Property(report => report.Id).HasColumnName("id");
        builder.Property(report => report.ReporterId).HasColumnName("reporter_id");
        builder.Property(report => report.PostId).HasColumnName("post_id");
        builder.Property(report => report.CommentId).HasColumnName("comment_id");
        builder.Property(report => report.Reason).HasColumnName("reason");
        builder.Property(report => report.Status).HasColumnName("status");
        builder.Property(report => report.ResolvedById).HasColumnName("resolved_by");
        builder.Property(report => report.ResolvedAt).HasColumnName("resolved_at");
        builder.Property(report => report.ResolutionReason).HasColumnName("resolution_reason").HasMaxLength(1000);

        builder.HasOne(report => report.Reporter)
            .WithMany()
            .HasForeignKey(report => report.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(report => report.Post)
            .WithMany()
            .HasForeignKey(report => report.PostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(report => report.Comment)
            .WithMany()
            .HasForeignKey(report => report.CommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(report => report.ResolvedBy)
            .WithMany()
            .HasForeignKey(report => report.ResolvedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
