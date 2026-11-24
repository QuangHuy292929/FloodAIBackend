using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class PostMediaConfiguration : IEntityTypeConfiguration<PostMedia>
{
    public void Configure(EntityTypeBuilder<PostMedia> builder)
    {
        builder.ToTable("post_media");

        builder.Property(media => media.Id).HasColumnName("id");
        builder.Property(media => media.PostId).HasColumnName("post_id");
        builder.Property(media => media.StorageKey).HasColumnName("storage_key");
        builder.Property(media => media.MediaType).HasColumnName("media_type");
        builder.Property(media => media.SortOrder).HasColumnName("sort_order");

        builder.HasOne(media => media.Post)
            .WithMany(related => related.Media)
            .HasForeignKey(media => media.PostId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
